using Frame.Domain.Entities.Core.Security;
using Microsoft.CodeAnalysis;

namespace Frame.Domain.Entities.Core.FrameNotifications
{
    public class FrameNotification : BaseEntity, IBaseGenericEntity<FrameNotification>
    {
        #region =================== Поля модели ===================
        /// <summary>
        /// Отправитель.
        /// </summary>
        virtual public User? Sender { get; set; }
        /// <summary>
        /// Id отправителя.
        /// </summary>
        public int? SenderId { get; set; }
        /// <summary>
        /// Получатель.
        /// </summary>
        virtual public User? Receiver { get; set; }
        /// <summary>
        /// Id получателя.
        /// </summary>
        public int? ReceiverId { get; set; }
        /// <summary>
        /// Тема сообщения.
        /// </summary>
        public string? Theme { get; set; }
        /// <summary>
        /// Сообщение.
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// Формат.
        /// </summary>
        public int FormatCode { get; set; }
        /// <summary>
        /// Непрочитанное.
        /// </summary>
        public bool IsNew { get; set; }
        #endregion

        #region =================== Метаданные ===================
        public List<Field<FrameNotification>> GetFields()
        {
            return Meta.Fields;
        }

        public static class Meta
        {
            public static readonly string HumanName = "Оповещение";
            public static readonly Field<FrameNotification> DateCreate = new() { Name = nameof(DateCreate), DateTimeGet = x => x.DateCreate, Required = true, HumanName = "Дата создания", Format="dd.MM.yyyy hh:mm" };
            public static readonly Field<FrameNotification> Theme = new() { Name = nameof(Theme), StringGet = x => x.Message, StringSet = (x, val) => x.Message = val ?? "", Required = true, HumanName = "Сообщение" };
            public static readonly Field<FrameNotification> Message = new() { Name = nameof(Message), StringGet = x => x.Theme, StringSet = (x, val) => x.Theme = val ?? "", Required = true, HumanName = "Тема" };
            public static readonly Field<FrameNotification> FormatCode = new() { Name = nameof(FormatCode), IntGet = x => x.FormatCode, IntSet = (x, val) => x.FormatCode = val ?? 0, Required = true, HumanName = "Формат" };
            public static readonly Field<FrameNotification> IsNew = new() { Name = nameof(IsNew), BoolGet = x => x.IsNew, BoolSet = (x, val) => x.IsNew = val ?? false, Required = true, HumanName = "Прочитано" };
            public static readonly RefField<FrameNotification, User> Sender = new()
            {
                Name = nameof(Sender),
                RefGetter = x => x.Sender,
                RefIdGetter = x => x.SenderId,
                RefSetter = (x, val) => x.Sender = val,
                RefIdSetter = (x, val) => x.SenderId = val,
                Required = true,
                HumanName = "Отправитель"
            };
            public static readonly RefField<FrameNotification, User> Receiver = new()
            {
                Name = nameof(Receiver),
                RefGetter = x => x.Receiver,
                RefIdGetter = x => x.ReceiverId,
                RefSetter = (x, val) => x.Receiver = val,
                RefIdSetter = (x, val) => x.ReceiverId = val,
                Required = true,
                HumanName = "Получатель"
            };

            public static readonly List<Field<FrameNotification>> Fields = [Sender, Receiver, DateCreate, Message, FormatCode, IsNew];
        }
        #endregion
    }
}
