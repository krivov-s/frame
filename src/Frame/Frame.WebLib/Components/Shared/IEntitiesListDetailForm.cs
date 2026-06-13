using Microsoft.AspNetCore.Components;

namespace Frame.WebLib.Components.Shared
{
    public interface IEntitiesListDetailForm
    {
        /// <summary>
        /// Событие, говорящее о том, что редактирование завершено. Неважно как, Save or Cancel. Время закрывать форму.
        /// </summary>
        public EventCallback OnCompleted { get; set; }
    }
}
