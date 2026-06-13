namespace Frame.App.Scripting
{
    public interface IExecuteReportScript
    {
        Task<object> ExecuteAsync(object globals);
    }
}
