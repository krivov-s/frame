using Frame.App.Cores;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.Infrastructure;

public static class SmartFormatHelpers
{
    public static Task<Result<object>> CreateContainerForSmartFormat(string paramListJson, AppCoreProvider? appCoreProvider)
    {
        ParamList localParamList = new();
        if (paramListJson.Length > 0)
        {
            localParamList.FromJson(paramListJson);
        }

        return CreateContainerForSmartFormat(localParamList, appCoreProvider);
    }

    public static async Task<Result<object>> CreateContainerForSmartFormat(ParamList paramList, AppCoreProvider? appCoreProvider)
    {
        if (appCoreProvider == null)
        {
            return Result<object>.Error("Отсутствует AppCoreProvider.");
        }
        // 1. AppCore
        Result<AppCore> resAppCore = await appCoreProvider.GetAppCoreAsync();
        if (resAppCore.IsError || resAppCore.Value == null)
        {
            return Result<object>.Error(resAppCore.ErrorResult);
        }

        // 2. Контейнер для передачи в Smart.Format
        var container = new
        {
            AppCore = resAppCore.Value,
            ParamList = paramList.AsDynDictionary()
        };
            
        return Result<object>.Success(container);
    }
    
    
}