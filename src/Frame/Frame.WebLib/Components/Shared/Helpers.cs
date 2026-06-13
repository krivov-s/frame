using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Params;
using Frame.Domain.QuerySpec;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Frame.WebLib.Components.Params;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Frame.WebLib.Components.Shared;

public static class Helpers
{
    private const string ErrFormat = "{Err}";
    
    public static async Task<Result<string?>> ShowAskDialogAsync(string dialogTitle, string fieldLabel, 
        string defaultValue, IDialogService? dialogService)
    {
        if (dialogService == null) return Result<string?>.Error($"Отсутствует {nameof(IDialogService)}");
        
        var parameters = new DialogParameters
        {
            ["DialogTitle"] = dialogTitle,
            ["FieldLabel"] = fieldLabel,
            ["DefaultValue"] = defaultValue
        };

        IDialogReference dialog = await dialogService.ShowAsync<AskDialog>(dialogTitle, parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            string userInput = result.Data?.ToString() ?? "";
            return Result<string?>.Success(userInput);
        }
        // Если пользователь нажал Cancel - возвращаем Success но с обнуленной строко.
        return Result<string?>.Success(null);
    }
        
    public static async Task<Result<TEntity>> ShowSelectDialogAsync<TEntity>(
        List<TEntity> entities,
        IDialogService? dialogService,
        ISnackbar? snackBar,
        string dialogTitle = "Выберите значение",
        string noDataMessage = "Отсутствуют записи для выбора")
        where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        if (dialogService == null)
        {
            return Result<TEntity>.Error("Не передан IDIalogService");
        }

        if (entities.Count == 0)
        {
            snackBar?.Add(noDataMessage, Severity.Info);
            return Result<TEntity>.Success(null);
        }
        
        DialogOptions options = new DialogOptions
            { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        DialogParameters<SelectDialog<TEntity>> parameters = new DialogParameters<SelectDialog<TEntity>>
        {
            { x => x.Items, entities }
        };

        var dialog = await dialogService.ShowAsync<SelectDialog<TEntity>>(dialogTitle, parameters, options);
        var r = await dialog.Result;
        if (r is { Canceled: false })
        {
            return Result<TEntity>.Success(r.Data as TEntity);
        }

        // Если пользователь нажал Cancel - возвращаем Success но без выбранного объекта.
        return Result<TEntity>.Success(null);
    }

    public static async Task<Result<TEntity>> ShowSelectDialogAsync<TEntity>(
        IObjectStorage? objectStorage,
        IDialogService? dialogService,
        ISnackbar? snackBar,
        string dialogTitle = "Выберите значение",
        string noDataMessage = "Отсутствуют записи для выбора",
        Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? configureQuerySpecification = null)
        where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        if (objectStorage == null)
        {
            return Result<TEntity>.Error("В метод ShowSelectDialogAsync не передан ObjectStorage");
        }
        
        Result<List<TEntity>> resEntities = await LoadData(objectStorage, configureQuerySpecification);
        if (resEntities.IsError || resEntities.Value == null)
        {
            snackBar?.Add(resEntities.ErrorResult);
            return Result<TEntity>.Error(resEntities.ErrorResult);
        }
            
        List<TEntity> entities = resEntities.Value;
        
        return await ShowSelectDialogAsync(entities, dialogService, snackBar, noDataMessage);
    }

    /// <summary>
    /// Заполнение списка параметров пользователем.  
    /// </summary>
    /// <returns>true - можно продолжать, false - пользователь отменил продолжение</returns>
    public static async Task<bool> FillParamListByUser(ParamList paramList,
        ISnackbar? snackBar,
        IDialogService? dialogService,
        ILogger? logger,
        string dialogTitle = "Параметры",
        string saveButtonText = "Выполнить")
    {
        if (paramList.CountVisible == 0)
        {
            return true;
        }

        if (dialogService == null)
        {
            string err = "Сервис диалога недоступен";
            logger?.LogError(err);
            snackBar?.Add(err, Severity.Error);
            return false;
        }

        try
        {
            DialogOptions options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                Position = DialogPosition.Center
            };

            DialogParameters parameters = new DialogParameters
            {
                { "ParamListJson", paramList.ToJson() },
                { "ShowSaveButton", true },
                { "ShowCancelButton", true },
                { "SaveButtonText", saveButtonText },
                { "CancelButtonText", "Отмена" }
            };

            var dialog = await dialogService.ShowAsync<ParamListDynamicForm>(dialogTitle, parameters, options);
            var result = await dialog.Result;

            if (result is { Canceled: false })
            {
                if (result.Data is string { Length: > 0 } resultData)
                {
                    paramList.FromJson(resultData);
                    return true;
                }
                else
                {
                    string err = "От пользователя вернулся некорректный список параметров";
                    logger?.LogError(err);
                    snackBar?.Add(err, Severity.Error);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            snackBar?.Add(ex.Message, Severity.Error);
            return false;
        }
    }

    public static async Task ExecuteScriptCommandAsync<TEntity>(ScriptCommand cmd,
                                                            HashSet<TEntity> setEntities,
                                                            IObjectStorage storage,
                                                            IDialogService dialogService,
                                                            IScriptCore scriptCore,
                                                            ILogger? logger, 
                                                            ISnackbar? snackBar)
    {
        try
        {
            ParamList paramList = new();
            paramList.FromJson(cmd.ParamListJson);
            bool bContinue = await FillParamListByUser(paramList, 
                snackBar, 
                dialogService, 
                logger, 
                "Параметры операции");
            if (!bContinue)
            {
                return;
            }

            List<dynamic> list = setEntities.Cast<dynamic>().ToList();
            Result<dynamic> res = await scriptCore.ExecuteScriptCommandAsync(cmd.Id, paramList, list, storage);
            if (res.IsError)
            {
                snackBar?.Add($"Ошибка выполнения команды: {res.ErrorResult}", Severity.Error);
            }
            else
            {
                string strError = (res.ErrorResult != "") ? res.ErrorResult : "Команда выполнена успешно.";
                snackBar?.Add(strError, Severity.Success);
            }
        }
        catch (Exception ex)
        {
            string err = $"Ошибка выполнения операции {cmd.ScriptName}: {ex.Message}";
            logger?.LogError(ex, err);
            snackBar?.Add(err, Severity.Error);
        }
    }
    
    
    public static async Task ExecuteReportAsync<TItem>(FrameReport report, List<TItem>? entities,
        IFrameReporting? frameReporting, ISnackbar? snackBar, IJSRuntime? jsRuntime, ILogger? logger, 
        IDialogService? dialogService)
    {
        string errPrefix = $"Отчет {report}";
        if (logger == null)
        {
            Console.WriteLine($"{errPrefix}: не передан logger");
            return;
        }
        if (snackBar == null)
        {
            logger.LogError("{Prefix}: не передан SnackBar", errPrefix);
            return;
        }
        if (frameReporting == null)
        {
            string err = $"{errPrefix}: отсутствует сервис FrameReporting";
            logger.LogError(ErrFormat, err);
            snackBar.Add(err, Severity.Error);
            return;
        }
        if (jsRuntime == null)
        {
            string err = $"{errPrefix}: отсутствует сервис JsRuntime";
            logger.LogError(ErrFormat, err);
            snackBar.Add(err, Severity.Error);
            return;
        }
        if (dialogService == null)
        {
            string err = $"{errPrefix}: отсутствует сервис IDialogService";
            logger.LogError(ErrFormat, err);
            snackBar.Add(err, Severity.Error);
            return;
        }

        if (report.ReportTemplate == null)
        {
            string err = $"{errPrefix}: не загружен или не установлен шаблон ReportTemplate";
            logger.LogError(ErrFormat, err);
            snackBar.Add(err, Severity.Error);
            return;
        }

        ParamList paramList = report.BuildParamList();
        bool bContinue = await Helpers.FillParamListByUser(paramList, 
            snackBar, 
            dialogService, 
            logger, 
            "Параметры отчета");

        if (!bContinue)
        {
            return;
        }

        List<dynamic>? dynamicList = entities?.Cast<dynamic>().ToList();
        
        Result<MemoryStream> resStream = await frameReporting.GenerateReportAsync(report, paramList, dynamicList);
        if (resStream.IsError)
        {
            snackBar.Add(resStream.ErrorResult, Severity.Error);
            return;
        }

        MemoryStream? data = resStream.Value;

        if(data == null || data.Length == 0)
        {
            string err = $"{errPrefix}: сервис подготовки отчетов не вернул никаких данных.";
            logger.LogError(err);
            snackBar.Add(err, Severity.Error);
            return;
        }

        logger.LogInformation("{Prefix}: отчет подготовлен. Размер данных: {DataSize}", errPrefix, data.Length);

        byte[] bytes =  data.ToArray();
        string fileName = $"Report_{report.ReportTemplate.FileName}";
        await jsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, Convert.ToBase64String(bytes));

        snackBar.Add("Формирование отчета завершено!", Severity.Success);
    }

    /// <summary>
    /// Вспомогательная функция скачивания файла из браузера клиента.
    /// На странице должна быть доступна js-функция downloadFileDirectly. Пример реализации:
    /// <code>
    /// window.downloadFileDirectly = async (url, fileName) => {
    ///     try
    ///     {
    ///         let response = await fetch(url);
    ///         if (!response.ok)
    ///         {
    ///             throw new Error(await response.text());
    ///         }
    ///
    ///         const blob = await response.blob();
    ///         const downloadUrl = window.URL.createObjectURL(blob);
    ///
    ///         const a = document.createElement('a');
    ///         a.href = downloadUrl;
    ///         a.download = fileName;
    ///         document.body.appendChild(a);
    ///         a.click();
    ///
    ///         window.URL.revokeObjectURL(downloadUrl);
    ///     }
    ///     catch (error)
    ///     {
    ///         console.error("Ошибка при скачивании файла:", error.message);
    ///         alert("Ошибка: " + error.message);
    ///     }
    /// };
    /// </code>
    /// </summary>
    /// <returns></returns>
    public static async Task<Result> DownloadFileAsync(IJSRuntime? jsRuntime, FileDocument? fileDocument, 
        ILogger? logger, CoreWebSettings? coreWebSettings)
    {
        if (fileDocument != null && jsRuntime != null)
        {
            if (string.IsNullOrEmpty(fileDocument.FileKey))
            {
                string err = "Ключ файла для загрузки из хранилища в объекте FileDocument не установлен.";
                logger?.LogError(ErrFormat, err);
                return Result.Error(err);
            }
            if (fileDocument.FileName.Length <= 0)
            {
                string err = "Имя файла для сохранения в объекте FileDocument не установлено.";
                logger?.LogError(ErrFormat, err);
                return Result.Error(err);
            }

            if (coreWebSettings == null)
            {
                string err = $"Не передан {coreWebSettings}";
                logger?.LogError(ErrFormat, err);
                return Result.Error(err);
            }

            if (string.IsNullOrEmpty(coreWebSettings.GetFileControllerPath))
            {
                string err = $"В {coreWebSettings} не прописан {nameof(coreWebSettings.GetFileControllerPath)}";
                logger?.LogError(ErrFormat, err);
                return Result.Error(err);
            }
            
            string url = $"{coreWebSettings.BasePath}{coreWebSettings.GetFileControllerPath}" +
                         $"?fileKey={Uri.EscapeDataString(fileDocument.FileKey)}" +
                         $"&fileName={Uri.EscapeDataString(fileDocument.FileName)}";

            try
            {
                await jsRuntime.InvokeVoidAsync("downloadFileDirectly", url, fileDocument.FileName);
                return Result.Success;
            }
            catch (Exception e)
            {
                string err = $"Ошибка выполнения загрузки файла в браузере на стороне клиента: {e.Message}";
                logger?.LogError(e, ErrFormat, err);
                return Result.Error(err);
            }
        }

        return Result.Error("Не установлены все необходимые параметры");
    }

    private static async Task<Result<List<TEntity>>> LoadData<TEntity>(
        IObjectStorage objectStorage,
        Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? configureQuerySpecification)
        where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        Result<IBaseRepository<TEntity>> result = objectStorage.GetBaseRepository<TEntity>();
        if (result.IsError || result.Value == null)
        {
            throw new FrameException("Не удалось получить репозиторий от IObjectStorage");
        }

        IBaseRepository<TEntity> repository = result.Value;

        if (configureQuerySpecification != null)
        {
            repository.ConfigureQuerySpecification = configureQuerySpecification;
        }

        return await repository.GetAllAsync();
    }
}
