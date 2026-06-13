using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;

namespace Frame.Domain.Entities.Core.Params
{
    public static class ParamListType
    {
        public static readonly string User = "User";
        public static readonly string System = "System";
    }

    /// <summary>
    /// Профиль пользователя. Включает в себя пользовательский список параметров, настройки интерфейса пользователя. 
    /// </summary>
    public class UserProfile : BaseEntity, IBaseGenericEntity<UserProfile>
    {
        public virtual User? User { get; set; }
        public int UserId { get; set; }
        
        /// <summary>
        /// Список параметров пользователя
        /// </summary>
        public string ParamListJson { get; set; } = "";
        
        /// <summary>
        /// Настройки интерфейса пользователя (например - предустановленные фильтры в списках). Пример:
        /// <code>
        /// {
        ///     "EntitiesListDetail": [
        ///         "ComponentName1": {
        ///             "FilterId": 1
        ///         },
        ///         "ComponentName2": {
        ///             "FilterId": null
        ///         }
        ///     ],
        ///     "SomeOtherSettings": [
        ///         "AnyOtherName" : "AnyOtherStructure"
        ///     ]
        /// }
        /// </code>
        /// Варианты использования, состав полей, могут определяться прикладным приложением.
        /// </summary>
        public string UIParamsJson { get; set; } = "";

        #region ========== Вычислимые атрибуты ==========

        /// <summary>
        /// Тип списка параметров (пользовательский/системный)
        /// </summary>
        /// <exception cref="Exception"></exception>
        public string ParamListType 
        { 
            get
            {
                if(User == null)
                {
                    throw new Exception("У ParamList не установлен User!");
                }
                return (User.Login == User.SystemUserName) 
                    ? Params.ParamListType.System 
                    : Params.ParamListType.User;
            }
        }
        

        #endregion


        /// <summary>
        /// Получить список параметров пользователя в виде объекта <see cref="ParamList"/>
        /// </summary>
        /// <returns></returns>
        public ParamList GetParamList()
        {
            ParamList paramList = new();
            if (ParamListJson.Length > 0)
            {
                paramList.FromJson(ParamListJson);
            }
            return paramList;
        }

        /// <summary>
        /// Установить список параметров пользователя. При установке он сконвертируется в Json и сохранится
        /// в атрибуте <see cref="ParamListJson"/>
        /// </summary>
        /// <param name="paramList"></param>
        public void SetParamList(ParamList paramList)
        {
            ParamListJson = paramList.ToJson();
        }

        public List<Field<UserProfile>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Профиль пользователя";
            public static readonly RefField<UserProfile, User> User = new()
            {
                Name = nameof(User),
                RefGetter = x => x.User,
                RefIdGetter = x => x.UserId,
                RefSetter = (x, val) => x.User = val!,
                RefIdSetter = (x, val) => x.UserId = val ?? 0,
                Required = false,
                HumanName = "Пользователь"
            };
            public static readonly Field<UserProfile> ParamListJson = new() { Name = nameof(ParamListJson), StringGet = x => x.ParamListJson, StringSet = (x, val) => x.ParamListJson = val ?? "", Required = false, HumanName = "Список параметров в JSon" };
            public static readonly Field<UserProfile> UIParamsJson = new() { Name = nameof(UIParamsJson), StringGet = x => x.UIParamsJson, StringSet = (x, val) => x.UIParamsJson = val ?? "", Required = false, HumanName = "Настройки интерфейса пользователя" };
            
            public static readonly List<Field<UserProfile>> Fields = [User, ParamListJson, UIParamsJson];
        }
    }
}
