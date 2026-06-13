using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;

namespace Frame.App.IEntityLoaders
{
    /// <summary>
    /// Этот интерфейс нужен для инициализации ScriptCore.
    /// </summary>
    public interface IEntityScriptLoader
    {
        /// <summary>
        /// Загрузка всех EntityScript.
        /// </summary>
        /// <returns><see cref="Role">Роль</see></returns>
        public Task<Result<List<EntityScript>>> LoadAllAsync();
    }
}
