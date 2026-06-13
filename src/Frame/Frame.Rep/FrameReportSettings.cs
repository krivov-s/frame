namespace Frame.Rep;

public class FrameReportSettings
{
    /// <summary>
    /// Путь к папке (относительно корня файл-сервера), в которой должны будут располагаться образцы отчетов.
    /// Этот путь будет использован при загрузке на сервер образцов отчетов. 
    /// </summary>
    public string ReportTemplatePath { get; set; } = "";
    public string ReportTempPath { get; set; } = "";
}
