using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Metadata;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Security
{
    public class UserSession
    {
        /// <summary>
        /// В конструктор должен приходить пользователь с уже подгруженными ролями, требованиями, списком параметров
        /// </summary>
        /// <param name="user"></param>
        public UserSession(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            User = user;
            try
            {
                UserParamList = user.UserProfile.GetParamList();
            }
            catch (FrameParamListSerializeException)
            {
                // Если произошла ошибка десериализации списка параметров - просто игнорируем ее,
                // и формируем новый список параметров
                UserParamList = new();
            }
            BuildSecurityMap();
        }

        public User User { get; }
        public ParamList UserParamList { get; }

        // Карта прав доступа пользователя к отдельным объектам системы
        private readonly Dictionary<string, SecurityProfile> _mapObjectTypeToSecCondition = [];

        /// <summary>
        /// Получения профиля безопасности <see cref="SecurityProfile"/> для заданного типа. Если в карте он отсутствует - возвращается null
        /// </summary>
        /// <returns></returns>
        public SecurityProfile? GetSecurityProfile<TEntity>()
        {
            return GetSecurityProfile(typeof(TEntity).Name);
        }
        
        /// <summary>
        /// Получения профиля безопасности <see cref="SecurityProfile"/> для заданного типа. Если в карте он отсутствует - возвращается null
        /// </summary>
        /// <param name="entityTypeName"></param>
        /// <returns></returns>
        public SecurityProfile? GetSecurityProfile(string entityTypeName)
        {
            _mapObjectTypeToSecCondition.TryGetValue(entityTypeName, out SecurityProfile? profile);
            return profile;
        }

        /// <summary>
        /// Построение карты безопасности для всех типов для текущего пользователя, 
        /// для которых в БД определены специальные настройки безопасности (по ролям)
        /// </summary>
        private void BuildSecurityMap()
        {
            // ------------------------------------------------------------------------------------
            // 1. Цикл по списку ролей для текущего пользователя. Перебираем роли, в каждой роли 
            // перебираем созданные в ней разрешения и суммируем их с существующими
            // ------------------------------------------------------------------------------------
            foreach (UsersRoles userRoleRel in User.UsersRoles)
            {
                if (userRoleRel.Role == null)
                {
                    throw new FrameException("Непонятная ошибка! В объекте-связке UsersRoles не установлена роль!");
                }
                Role role = userRoleRel.Role;
                List<TEntityRights> entityRights = role.EntityRights;
                foreach (TEntityRights entityRight in entityRights)
                {
                    if (!_mapObjectTypeToSecCondition.TryGetValue(entityRight.EntityTypeName, out SecurityProfile? profile))
                    {
                        // Для данного типа в карте профиля нет, создаем новый
                        profile = new();
                    }
                    profile.AddFilters(entityRight);
                    // Сохраняем созданный или модифицированный профиль в карту
                    _mapObjectTypeToSecCondition[entityRight.EntityTypeName] = profile;
                }
            }
            
            // ------------------------------------------------------------------------------------
            // 2. Проходимся по всем типам из Domain и проверяем, создана ли запись в карте.
            // Для тех, для которых запись не создана - создаем и выдаем все права. 
            // Таким образом получается, что для всех типов, для которых выше в роли были 
            // заданы ограничения - они останутся без изменений.
            // Для всех прочих типов, для которых ничего не было задано - будут выданы полные права.
            // Таким образом реализуется правило "по-умолчанию все разрешено".
            // Если нужно будет перейти к правилу "по-умолчанию все запрещено" - нужно будет просто
            // закомментить следующий цикл.
            // ------------------------------------------------------------------------------------
            List<string> types = EntityMetadata.GetAllEntitiesClassNames();
            foreach (string typeName in types)
            {
                if (!_mapObjectTypeToSecCondition.TryGetValue(typeName, out SecurityProfile? profile))
                {
                    // Для данного типа в карте профиля нет, создаем новый
                    profile = new();
                    // Предоставляем полные права в те поля, которые не были затронуты.
                    profile.GrantAllToEmpty();
                    _mapObjectTypeToSecCondition[typeName] = profile;
                }
            }
            
        }

    }
}
