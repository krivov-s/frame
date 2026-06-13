// using System.Data;
// using System.Reflection;
// using FastReport.Data;
// using FastReport.Export.Image;
// using FastReport.Export.PdfSimple;
// using FastReport.Web;
// using Frame.App.IEntityRepositories;
// using Frame.Domain;
// using Frame.Domain.Entities.Core.Security;
// using Frame.Shared;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
//
//
// namespace Frame.Rep.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class FastReportController(IObjectStorage objectStorage, ILogger<FastReportController> logger) : Controller
//     {
//         [HttpGet("About")]
//         public IActionResult About()
//         {
//             return Ok("Сервис построения отчетов FastReport");
//         }
//
//         [HttpGet("ShowReport")]
//         public async Task<IActionResult> ShowReport(string reportFile, string reportParams)
//         {
//             try
//             {
//                 WebReport webReport = await GetReportObjectAsync(reportFile, reportParams);
//                 await webReport.Report.PrepareAsync();
//                 webReport.Report.SavePrepared(Path.Combine("c:\\temp\\", "test_prepared.fpx"));
//                 ImageExport image = new ImageExport();
//                 image.ImageFormat = ImageExportFormat.Jpeg;
//                 webReport.Report.Export(image, Path.Combine("c:\\temp\\", "report.jpg"));
//
//                 ViewBag.Export2PdfUrl = "/report/topdf?reportfile=" + reportFile + "&reportparams=" + reportParams;
//
//                 webReport.Toolbar.ShowPrint = true;
//                 return View(webReport);
//             }
//             catch(Exception ex)
//             {
//                 logger.LogError(ex, "Ошибка при подготовке отчета: {Err}", ex.Message);
//                 return BadRequest(ex.Message);
//             }
//         }
//         
//         [HttpGet("ToPdf")]
//         public async Task<IActionResult> ToPdf(string reportfile, string reportparams)
//         {
//             try
//             {
//                 var webReport = await GetReportObjectAsync(reportfile, reportparams);
//                 webReport.Report.Prepare();
//
//                 using (MemoryStream ms = new MemoryStream())
//                 {
//                     PDFSimpleExport pdfExport = new PDFSimpleExport();
//                     pdfExport.Export(webReport.Report, ms);
//                     ms.Flush();
//                     return File(ms.ToArray(), "application/pdf", Path.GetFileNameWithoutExtension(webReport.ReportName) + ".pdf");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 return BadRequest(ex.Message);
//             }
//         }
//
//         private async Task<WebReport> GetReportObjectAsync(string reportFile, string reportParams)
//         {
//             WebReport webReport = null;
//
//             if (reportFile != null && reportFile.Length > 0)
//             {
//                 try
//                 {
//                     webReport = new WebReport();
//                     string sFilePath = AppContext.BaseDirectory + "/Reports/" + reportFile;
//                     webReport.Report.Load(sFilePath);
//
//                     var ds = webReport.Report.GetDataSource("Users");
//                     Result<List<User>> resUser = await objectStorage.GetListAsync<User>("");
//                     if (!resUser.IsError && resUser.Value != null)
//                     {
//                         webReport.Report.RegisterData(resUser.Value, "Users", 3);
//                         var ds1 = webReport.Report.GetDataSource("Users");
//                         ds1.Enabled = true;
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     throw new Exception("Ошибка чтения файла отчета: " + ex.Message);
//                 }
//                 // if (webReport.Report.Parameters.Count > 0)
//                 // {
//                 //     if (reportparams != null)
//                 //     {
//                 //         List<FRParameter> lstParams = JsonSerializer.Deserialize<List<FRParameter>>(reportparams);
//                 //         foreach (Parameter pp in webReport.Report.Parameters)
//                 //         {
//                 //             FRParameter frp = lstParams.FirstOrDefault(s => s.Name == pp.Name);
//                 //             if (frp != null)
//                 //             {
//                 //                 webReport.Report.SetParameterValue(pp.Name, frp.Value);
//                 //             }
//                 //         }
//                 //     }
//                 // // }
//             }
//             return webReport;
//         }
//         
//         public static DataTable ToDataTable<T>(List<T> items)
//         {
//             var dataTable = new DataTable(typeof(T).Name);
//             var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
//
//             foreach (var prop in properties)
//             {
//                 if (!prop.CanRead) continue;
//                 dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
//             }
//
//             foreach (var item in items)
//             {
//                 var row = dataTable.NewRow();
//                 foreach (var prop in properties)
//                 {
//                     if (!prop.CanRead) continue;
//                     row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
//                 }
//                 dataTable.Rows.Add(row);
//             }
//
//             return dataTable;
//         }
//         
//     }
// }
