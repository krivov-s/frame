namespace Frame.App.Messaging
{
    /// <summary>
    /// Класс, собирающий информацию для отправки Email
    /// </summary>
    public class EmailSettings
    {
        /// <summary>
        /// Имя отправителя.
        /// </summary>
        public string? SenderName { get; set; }

        /// <summary>
        /// Почта отправителя.
        /// </summary>
        public string? SenderEmail { get; set; }

        /// <summary>
        /// Словарь почт и имён получателей, где ключом является почта, а имя - значением.
        /// </summary>
        public Dictionary<string, string> Recepients { get; set; } = [];

        /// <summary>
        /// Словарь почт и имён копий отправки, где ключом является почта, а имя - значением.
        /// </summary>
        public Dictionary<string, string> Copies { get; set; } = [];

        /// <summary>
        /// Тема письма.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Текст письма.
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// Тело письма.
        /// </summary>
        public string? HtmlBody { get; set; }

        /// <summary>
        /// Вложения(файлы). Словарь, в котором ключом является имя файла, а значением - массив байтов.
        /// </summary>
        public Dictionary<string, byte[]> Attachments { get; set; } = [];

        public string? Server{ get; set; }
        public int Port { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
    }
}
