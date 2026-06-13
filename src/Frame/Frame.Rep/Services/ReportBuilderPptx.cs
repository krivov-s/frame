using DocumentFormat.OpenXml.Packaging;
using Frame.App.Cores;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Params;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Microsoft.Extensions.Logging;
//Внутри библиотеки DocumentFormat.OpenXml находятся классы с одинаковыми названиям в разных пространствах имён
//поэтому нагляднее добавить ссылки через синонимы.
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Westwind.Scripting;

namespace Frame.Rep.Services
{
    internal class ReportBuilderPptx(ILogger<IReportBuilder> logger, IFilesRepository filesRepository,
        AppCoreProvider appCoreProvider, IObjectStorageProvider objectStorageProvider,
        IScriptCore scriptCore) : IReportBuilder

    {
        private const string _imageTag = "@image";
        private const string _keywordRow = "row";

        private readonly ILogger<IReportBuilder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IFilesRepository _filesRepository = filesRepository ?? throw new ArgumentNullException(nameof(filesRepository));
        private readonly AppCoreProvider _appCoreProvider = appCoreProvider ?? throw new ArgumentNullException(nameof(appCoreProvider));
        private readonly IObjectStorageProvider _objectStorageProvider = objectStorageProvider ?? throw new ArgumentNullException(nameof(objectStorageProvider));
        private readonly IScriptCore _scriptCore = scriptCore ?? throw new ArgumentNullException(nameof(scriptCore));
        private readonly CSharpScriptExecution exec = new() { SaveGeneratedCode = true };

        public async Task<Result<MemoryStream>> GenerateReportAsync(FrameReport report, ParamList paramList, List<dynamic>? entities = null, IObjectStorage? storage = null)
        {
            string logPrefix = (report.Name.Length > 0)
                                ? $"Отчет {report.Name}"
                                : $"Отчет с Id = {report.Id}";

            try
            {
                Result<MemoryStream> resTemplate = await ReadReportTemplateToMemoryAsync(logPrefix, report);
                if (resTemplate.IsError || resTemplate.Value == null)
                {
                    return resTemplate;
                }
                await ReportHelpers.DebugLogAsync($"Шаблон считан", report, _logger, _objectStorageProvider, false);

                MemoryStream memoryStreamSource = resTemplate.Value;

                // Копирование необходимо, т.к. это будет новый документ, да еще и с доп. строками
                MemoryStream memoryStream = new();
                await memoryStreamSource.CopyToAsync(memoryStream);

                await ReportHelpers.DebugLogAsync("Начало формирования", report, _logger, _objectStorageProvider, true, true);
                Result<List<dynamic>> resList = await ReportHelpers.LoadData(logPrefix, report, paramList, _logger, _scriptCore, _objectStorageProvider, storage, entities);
                if (resList.IsError || resList.Value == null)
                {
                    return Result<MemoryStream>.Error(resList);
                }
                await ReportHelpers.DebugLogAsync($"Данные прочитаны", report, _logger, _objectStorageProvider, true);

                List<dynamic> list = resList.Value;
                _logger.LogInformation("{LogPrefix}: получено {Count} записей.", logPrefix, list.Count);

                string errorPrefix = $"Подготовка презентации {report.Name}";
                List<string> imageScripts = [];

                Result<AppCore> appCore = await _appCoreProvider.GetAppCoreAsync();
                if (appCore.IsError)
                {
                    return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения AppCore", _logger);
                }

                //Создание глобальных переменных для выполнения скриптов в режиме
                ScriptExecutionGlobals globals = new()
                {
                    AppCore = appCore.Value,
                    Obj = (list.Count > 0) ? list[0] : null,
                    ParamList = paramList.AsDynDictionary()
                };

                #region Работа с текстами и таблицами в презенации

                //Начало работы с презентацией
                using (PresentationDocument presentation = PresentationDocument.Open(memoryStream, true))
                {
                    PresentationPart? presentationPart = presentation.PresentationPart;
                    if (presentationPart == null)
                    {
                        return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения PresentationDocument", _logger);
                    }

                    P.SlideIdList? slideIdLists = presentationPart.Presentation.SlideIdList;
                    if (slideIdLists == null)
                    {
                        return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения SlideIdList", _logger);
                    }

                    //Получение всех слайдов презентации для работы
                    foreach (P.SlideId? item in slideIdLists.Cast<P.SlideId?>())
                    {
                        if (item == null)
                            continue;

                        string? relationshipId = item.RelationshipId;
                        if (string.IsNullOrEmpty(relationshipId))
                            continue;

                        OpenXmlPart? part = presentationPart.GetPartById(relationshipId);
                        if (part == null)
                        {
                            return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения OpenXmlPart", _logger);
                        }

                        if (part is not SlidePart slidePart)
                        {
                            return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения OpenXmlPart", _logger);
                        }

                        P.Slide? slide = slidePart.Slide;
                        if (slide == null)
                        {
                            return ReportHelpers.LogAndReturnError(errorPrefix, "ошибка получения Slide", _logger);
                        }

                        //Дополнительное логгирование
                        _logger.LogInformation("Слайд открыт для редактирования.");

                        //Работа с таблицами. Проверяем каждую таблицу на наличие тэгов для заполнения.
                        A.Table[] tables = slide.Descendants<A.Table>().ToArray();

                        if (tables.Length != 0)
                        {
                            foreach (A.Table table in tables)
                            {
                                FillTable(table, globals.Obj);
                            }
                        }

                        A.Text[] descendants = slide.Descendants<A.Text>().ToArray();

                        foreach (A.Text text in descendants)
                        {
                            if (text.Text.Contains(_imageTag))
                            {
                                imageScripts.Add(text.Text);
                            }
                            else
                            {
                                //Дополнительное логгирование
                                _logger.LogInformation($"Выполнение скрипта{text.Text}");
                                object? executionResult = await ReportHelpers.ExecuteStringWithScript(text.Text, globals, report, _imageTag, _logger, _objectStorageProvider, scriptCore);

                                text.Text = FormatNumberWithSeparator(executionResult);
                            }
                        }
                    }
                }
                #endregion

                #region Работа с изображениями в презентации

                //Дополнительное логгирование
                _logger.LogInformation("Начало работы с изображениями.");

                foreach (string imageScript in imageScripts)
                {
                    await TryChangeShapeForPicture(memoryStream, imageScript, globals, report);
                }

                #endregion

                return Result<MemoryStream>.Success(memoryStream);
            }
            catch (Exception exc)
            {
                string err = $"Ошибка при формировании презентации {report}: {exc.Message}";
                _logger.LogError(exc, err);
                return Result<MemoryStream>.Error(err);
            }
        }

        private async Task<Result<MemoryStream>> ReadReportTemplateToMemoryAsync(string logPrefix, FrameReport report)
        {
            if (report.ReportTemplate == null)
            {
                string err = $"{logPrefix}: в объекте FrameReport не загружен файл-документ с шаблоном отчёта";
                _logger.LogError("{Err}", err);
                return Result<MemoryStream>.Error(err);
            }

            if (string.IsNullOrEmpty(report.ReportTemplate.FileKey))
            {
                string err = $"{logPrefix}: в файл-документе отсутствует путь до шаблона отчёта (FileKey)";
                _logger.LogError("{Err}", err);
                return Result<MemoryStream>.Error(err);
            }

            Result<byte[]> fileRepositoryResult = await _filesRepository.GetFileAsync(report.ReportTemplate.FileKey);

            if (fileRepositoryResult.IsErrorOrNull)
            {
                return Result<MemoryStream>.Error(fileRepositoryResult.ErrorResult);
            }

            MemoryStream memoryStreamSource = new(fileRepositoryResult.Value!);
            return Result<MemoryStream>.Success(memoryStreamSource);
        }

        /// <summary>
        /// Метод возвращает true в случае корректного добавления изображения в форму и false при ошибке.
        /// </summary>
        /// <param name="textOnShape">Элемент "Текст", который прикреплён к элементу "Форма",
        /// в который будем добавлять изображение.</param>
        /// <param name="script">Скрипт для поиска среди элементов A.Text</param>
        /// <param name="globals">Глобальные переменные.</param>
        /// <param name="report">Отчёт.</param>
        /// <returns></returns>
        private async Task<bool> TryChangeShapeForPicture(MemoryStream ms, string script, ScriptExecutionGlobals globals, FrameReport report)
        {
            using PresentationDocument presentation = PresentationDocument.Open(ms, true);

            PresentationPart? presentationPart = presentation.PresentationPart;
            if (presentationPart == null)
            {
                ReportHelpers.LogAndReturnError("", "ошибка получения PresentationDocument", _logger);
                return false;
            }

            P.SlideIdList? slideIdLists = presentationPart.Presentation.SlideIdList;
            if (slideIdLists == null)
            {
                ReportHelpers.LogAndReturnError("", "ошибка получения SlideIdList", _logger);
                return false;
            }

            //Получение всех слайдов презентации для работы
            foreach (P.SlideId? item in slideIdLists.Cast<P.SlideId?>())
            {
                var test = item?.Descendants().ToArray();

                if (item == null)
                    continue;

                string? relationshipId = item.RelationshipId;
                if (string.IsNullOrEmpty(relationshipId))
                    continue;

                OpenXmlPart? part = presentationPart.GetPartById(relationshipId);
                if (part == null)
                {
                    ReportHelpers.LogAndReturnError("", "ошибка получения OpenXmlPart", _logger);
                    continue;
                }

                if (part is not SlidePart slidePart)
                {
                    ReportHelpers.LogAndReturnError("", "ошибка получения OpenXmlPart", _logger);
                    continue;
                }

                P.Slide? slide = slidePart.Slide;
                if (slide == null)
                {
                    ReportHelpers.LogAndReturnError("", "ошибка получения Slide", _logger);
                    continue;
                }

                A.Text? selectedScriptText = slide.Descendants<A.Text>().FirstOrDefault(x => x.Text == script);
                if (selectedScriptText == null)
                    continue;

                //Получаем элемент "Форма" из элемента текст
                P.Shape? shape = GetShapeFromText(selectedScriptText);
                if (shape == null)
                    return false;

                //Получаем элемент "Дерево форм"
                if (shape.Parent is not P.ShapeTree shapeTree)
                    return false;

                A.Offset? shapeOffset = shape.Descendants<A.Offset>().FirstOrDefault();
                if (shapeOffset == null)
                    return false;

                A.Extents? shapeExtents = shape.Descendants<A.Extents>().FirstOrDefault();
                if (shapeExtents == null)
                    return false;

                P.Picture picture = new();
                P.NonVisualPictureProperties nvPicPr = new();

                object? executionResult = await ReportHelpers.ExecuteStringWithScript(selectedScriptText.Text, globals, report, _imageTag, _logger, _objectStorageProvider, _scriptCore);
                string fileKey = executionResult?.ToString() ?? "";

                if (string.IsNullOrEmpty(fileKey))
                {
                    selectedScriptText.Text = string.Empty;
                    return false;
                }
                    

                byte[]? bytesOfFile = null;
                if (fileKey.Contains(".jpg"))
                {
                    using FileStream fs = new(fileKey, FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer);
                    bytesOfFile = buffer;
                }
                else
                {
                    bytesOfFile = await _filesRepository.GetFileAsync(fileKey);
                }

                if (bytesOfFile == null)
                    return false;

                //Поток данных с изображением.
                Stream mStream = new MemoryStream(bytesOfFile);

                //Добавляем раздел-изображение в элементы в документе.
                ImagePart imagePart = slidePart.AddImagePart(ImagePartType.Jpeg); // Или другой тип, соответствующий вашему изображению

                //Заполняем раздел-изображение данными из стрима.
                imagePart.FeedData(mStream);

                //Параметр "заполнение"
                P.BlipFill blipFill = new();

                A.Blip blip = new()
                {
                    //Добавляем ссылку на раздел-изображение
                    Embed = slidePart.GetIdOfPart(imagePart)
                };
                A.Stretch stretch = new();

                //Свойства элемента "форма"
                P.ShapeProperties spPr = new();
                A.Transform2D xfrm = new();
                //Размеры
                A.Offset off = new()
                {
                    X = shapeOffset.X,
                    Y = shapeOffset.Y
                };
                //Расширение
                A.Extents ext = new()
                {
                    Cx = shapeExtents.Cx,
                    Cy = shapeExtents.Cy,
                };
                //Тип формы
                A.PresetGeometry prstGeom = new()
                {
                    Preset = A.ShapeTypeValues.Rectangle
                };
                A.AdjustHandleList avList = new();

                //Смещение
                A.Outline ln = new()
                {
                    Width = 0
                };
                //Заполнение цветом
                A.FillRectangle fillRectangle = new();
                ln.AppendChild(fillRectangle);

                Random rnd = new();

                uint randomId = (uint)rnd.Next(1000, 100000);
                //Невизульные свойства(можно добавить надпись)
                A.NonVisualDrawingProperties cNvPr = new()
                {
                    Id = randomId
                };

                //Собираем дерево изображения
                prstGeom.AppendChild(avList);

                xfrm.AppendChild(off);
                xfrm.AppendChild(ext);

                spPr.AppendChild(xfrm);
                spPr.AppendChild(prstGeom);
                spPr.AppendChild(ln);

                nvPicPr.AppendChild(cNvPr);

                blipFill.AppendChild(blip);
                blipFill.AppendChild(stretch);

                picture.AppendChild(nvPicPr);
                picture.AppendChild(blipFill);
                picture.AppendChild(spPr);

                shapeTree.ReplaceChild(picture, shape);

                slidePart.Slide.Save();

                return true;
            }

            return true;
        }

        /// <summary>
        /// Метод для нахождения элемента shape из элемента text
        /// </summary>
        /// <param name="text">Элемент, среди родителей которого ищем форму</param>
        private static P.Shape? GetShapeFromText(A.Text text)
        {
            if (text.Parent is not A.Run run)
                return null;

            if (run.Parent is not A.Paragraph paragraph)
                return null;

            if (paragraph.Parent is not P.TextBody textBody)
                return null;

            if (textBody.Parent is not P.Shape shape)
                return null;

            return shape;
        }

        /// <summary>
        /// Провкерка таблицы на необходимость заполнения и заполенение.
        /// </summary>
        /// <param name="table">Таблица внутри презентации.</param>
        /// <param name="source">Данные для таблицы.</param>
        private void FillTable(A.Table table, dynamic source)
        {
            if (source == null)
                return;

            PropertyInfo[] propertyInfos = source
                                            .GetType()
                                            .GetProperties();

            List<A.TableRow> tableRows = table.Descendants<A.TableRow>().ToList();
            List<dynamic> sourceFromObj = [];
            Dictionary<int, string> columnIndexes = [];
            A.TableCell? styleCell = null;

            //Заполняем первые строки, содержащие ключевые слова для заполнения.
            //По завершению метода получим информацию об индексах строк для заполнения пустых.
            foreach (A.TableRow row in tableRows)
            {
                if (string.IsNullOrEmpty(row.InnerText))
                    continue;

                List<A.TableCell> tableCells = row.Descendants<A.TableCell>().ToList();

                //В свойствах классов OpenXML внутри элемента Table и вложенных элементов TableRow и TableCell
                //не нашёл ссылок на колонки, поэтому запонлнять таблицу будем с помощью цикла for


                foreach (A.TableCell cell in tableCells)
                {
                    //Проверяем: если TableRow.InnerText пуст, а в словаре columnIndexes есть какие-то записи, значит


                    //1. Определяем, есть ли в ячейке свойство для заполнения таблицы. Пример: Tests[]
                    string elementInnerText = cell.InnerText;

                    Match match = Regex.Match(elementInnerText, @"\.([^.]*\[])\.");
                    if (!match.Success)
                        continue;

                    //2. Если нашли свойство, ищем его среди свойств нашего объекта Obj.
                    string stringMatch = match.Value;
                    stringMatch = stringMatch.Trim('.').Replace("[", "").Replace("]", "");

                    PropertyInfo? matchResult = propertyInfos.FirstOrDefault(x => x.Name == stringMatch);
                    if (matchResult == null)
                        continue;

                    //3. Ищем в объекте Obj свойство являющееся массивом и называющееся также как свойство в ячейке. 
                    string cellPropertyName = elementInnerText.Split('.').Last();
                    IEnumerable<dynamic> propertyArrayValues = source.GetType().GetProperty(stringMatch).GetValue(source, null);
                    dynamic firstArrayItem = propertyArrayValues.First();
                    //Ошибка при некорректно входящем поле
                    string firstArrayItemValue = firstArrayItem.GetType().GetProperty(cellPropertyName).GetValue(firstArrayItem, null).ToString();

                    if (sourceFromObj.Count == 0)
                        sourceFromObj = new List<dynamic>(propertyArrayValues);


                    if (propertyArrayValues != null && matchResult.PropertyType.IsArray)
                    {
                        styleCell ??= cell;

                        ChangeCellText(cell, styleCell, firstArrayItemValue);
                        columnIndexes.Add(tableCells.IndexOf(cell), cellPropertyName);
                    }
                }
            }

            //Используя информацию из словаря заполняем пустые строки.
            A.TableRow[] emptyRows = tableRows.Where(x => string.IsNullOrEmpty(x.InnerText)).ToArray();

            if (emptyRows.Length == 0)
                return;

            if (columnIndexes.Count == 0)
                return;

            //Пропускаем первую запись, т.к. уже записали её на этапе работы с ключевыми словами в таблице.
            sourceFromObj = sourceFromObj.Skip(1).ToList();

            int rowsLength = 0;
            if (emptyRows.Length < sourceFromObj.Count)
            {
                rowsLength = emptyRows.Length;
            }
            else
            {
                rowsLength = sourceFromObj.Count;
            }

            for (var i = 0; i < rowsLength; i++)
            {
                A.TableCell[] emptyCellsToFill = emptyRows[i].Descendants<A.TableCell>().ToArray();

                foreach (KeyValuePair<int, string> keyValue in columnIndexes)
                {
                    dynamic stringToFill = sourceFromObj[i];
                    string valueToCell = stringToFill.GetType().GetProperty(keyValue.Value).GetValue(stringToFill, null).ToString();

                    A.TableCell cellToFill = emptyCellsToFill[keyValue.Key];

                    ChangeCellText(cellToFill, styleCell, valueToCell);
                }
            }
        }

        /// <summary>
        /// Заменяет свойство InnerText в элементе TableCell.
        /// </summary>
        /// <param name="cell">Ячейка таблицы.</param>
        /// <param name="styleCell">Ячейка, из которой будут заимствованы стили.</param>
        /// <param name="text">Текст для замены.</param>
        private void ChangeCellText(A.TableCell cell, A.TableCell? styleCell, string text)
        {
            //Правильно указать на необходимость ячеейки для стилей.
            if (styleCell == null)
                return;

            A.Run? run = cell.Descendants<A.Run>().FirstOrDefault();
            if (run == null)
            {
                A.TextBody textBody = new();
                var styleParProperties = styleCell.Descendants<A.ParagraphProperties>().FirstOrDefault();
                if (styleParProperties == null)
                {
                    string err = "Текст ячеек таблицы в презентации не изменён: отсутствуют стили в шаблоне презентации";
                    _logger.LogError(err);
                    return;
                }

                A.ParagraphProperties? parProp = new()
                {
                    Alignment = styleParProperties.Alignment,
                    DefaultTabSize = styleParProperties.DefaultTabSize,
                    FontAlignment = styleParProperties.FontAlignment,
                    Height = styleParProperties.Height,
                    LeftMargin = styleParProperties.LeftMargin,
                    RightMargin = styleParProperties.RightMargin,
                    RightToLeft = styleParProperties.RightToLeft,
                    SpaceAfter = styleParProperties.SpaceAfter,
                    SpaceBefore = styleParProperties.SpaceBefore
                };

                //Настройка тэга <rPr/> в текстовром элементе презентации.
                A.RunProperties? styleRunProp = styleCell.Descendants<A.RunProperties>().FirstOrDefault();
                A.RunProperties runProp = new()
                {
                    FontSize = styleRunProp?.FontSize
                };

                //Настройка тэга <r/> в текстовром элементе презентации.
                A.Run newRun = new();
                A.Text newText = new()
                {
                    Text = text
                };

                newRun.Append(newText);

                //Настройка тэга <p/> в текстовром элементе презентации.
                A.Paragraph par = new()
                {
                    ParagraphProperties = parProp
                };

                //Добавление собранной ветки в 
                newRun.Append(runProp);
                par.Append(newRun);
                textBody.Append(par);
                cell.Append(textBody);

                return;
            }

            A.Text? textRun = run.Descendants<A.Text>().FirstOrDefault();
            if (textRun == null)
                return;

            textRun.Text = text;
        }

        private static string FormatNumberWithSeparator(dynamic? data, string separator = " ")
        {
            if(data == null)
                return string.Empty;

            if(data is decimal)
            {
                NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                nfi.NumberGroupSeparator = separator;

                return data.ToString("#,0.00" , nfi);
            }

            if(data is int)
            {
                NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                nfi.NumberGroupSeparator = separator;

                return data.ToString("#,000", nfi);
            }

            return data.ToString();
        }
    }
}
