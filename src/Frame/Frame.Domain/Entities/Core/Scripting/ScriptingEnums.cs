using Frame.Shared;

namespace Frame.Domain.Entities.Core.Scripting
{
    /// <summary>
    /// Чем является операция: командой или источником данных
    /// </summary>
    public enum ECommandType
    {
        /// <summary>
        /// Операция является командой (функцией), выполняет требуемые действия и возращает <see cref="Result{T}"/>
        /// </summary>
        Command,
        
        /// <summary>
        /// Операция является источником данных: код формирует и возвращает IQuerable{TEntity}
        /// </summary>
        DataSource
    }
    
    /// <summary>
    /// 
    /// </summary>
    public enum ECodeType
    {
        SimpleMethod,
        Class
    }

    public enum EThreadType
    {
        //        SimpleMethodSync,
        //        SimpleMethodAsync,
        Sync,
        Async
    }

    public enum EHookType
    {
        OnBeforeSave,
//        OnBeforeSaveAsync,
        OnAfterSave,
//        OnAfterSaveAsync,
        OnBeforeDelete,
//        OnBeforeDeleteAsync,
        OnAfterDelete,
//        OnAfterDeleteAsync
    }

    public static class ESelectListItems
    {
        public static readonly List<SelectListItem> CommandTypes = Enum.GetValues<ECommandType>()
                .Select(ee => new SelectListItem()
                {
                    Value = (int)ee,
                    Description = Enum.GetName(ee) ?? $"Некорректное значение {nameof(ECommandType)}"
                })
                .ToList();

        public static readonly List<SelectListItem> CodeTypes = Enum.GetValues<ECodeType>()
                .Select(ee => new SelectListItem()
                {
                    Value = (int)ee,
                    Description = Enum.GetName(ee) ?? $"Некорректное значение {nameof(ECodeType)}"
                })
                .ToList();

        public static readonly List<SelectListItem> ThreadTypes = Enum.GetValues<EThreadType>()
                .Select(ee => new SelectListItem()
                {
                    Value = (int)ee,
                    Description = Enum.GetName(ee) ?? $"Некорректное значение {nameof(EThreadType)}"
                })
                .ToList();

        public static readonly List<SelectListItem> HookTypes = Enum.GetValues<EHookType>()
                .Select(ee => new SelectListItem()
                {
                    Value = (int)ee,
                    Description = Enum.GetName(ee) ?? $"Некорректное значение {nameof(EHookType)}"
                })
                .ToList();
    }
}
