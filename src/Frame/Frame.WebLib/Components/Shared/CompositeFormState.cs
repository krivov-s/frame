using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Frame.WebLib.Components.Shared
{
    public class CompositeFormState
    {
        /// <summary>
        /// Событие, говорящее о том, что состояние формы изменилось
        /// </summary>
        public EventCallback OnStateChanged { get; set; }

        public bool IsDirty => _isDirty;
        private bool _isDirty;

        /// <summary>
        /// Установить у формы флаг того, что данные изменились (или не изменились) и оповестить подписчиков
        /// </summary>
        /// <param name="value"></param>
        public void SetDirty(bool value = false)
        {
            _isDirty = value;
            OnStateChanged.InvokeAsync();
        }

        //private void NotifyStateChanged() => OnStateChanged.Invoke();

        /// <summary>
        /// Вспомогательный метод проверки объекта CompositeFormState с выдачей сообщения пользователю
        /// </summary>
        /// <param name="state"></param>
        /// <param name="snackBar"></param>
        /// <returns></returns>
        public static bool CheckFormSate(CompositeFormState? state, ISnackbar? snackBar)
        {
            if(state == null)
            {
                snackBar?.Add($"Не передан {nameof(CompositeFormState)}");
                return false;
            }

            return true;
        }
    }
}
