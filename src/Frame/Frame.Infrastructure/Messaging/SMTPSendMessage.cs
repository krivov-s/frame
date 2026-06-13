using Frame.App.IEntityRepositories;
using Frame.App.Messaging;
using Frame.Shared;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Frame.Infrastructure.Messaging
{
    public class SMTPSendMessage(IObjectStorageProvider objectStorageProvider,
    ILogger<SMTPSendMessage> logger) : ISMTPSendMessage
    {
        public async Task<Result> SendMessage(EmailSettings emailSettings)
        {
            string[] validationErrors = ValidateEmail(emailSettings);
            if (validationErrors.Length != 0)
            {
                string err = validationErrors.Aggregate((first, second) => $"{first}; {second}");
                logger.LogError(err);
            }

            // Создаем сообщение
            var message = new MimeMessage();

            // От кого (можно указать имя и email)
            message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));

            foreach(KeyValuePair<string, string> recepient in emailSettings.Recepients)
            {
                //Ключом в этом случае является почта, т.к. имена теоретически могут повторяться,
                //а почта должна быть уникальна для домена
                message.To.Add(new MailboxAddress(recepient.Value, recepient.Key));
            }

            if(emailSettings.Copies != null || emailSettings.Copies?.Count == 0)
            {
                foreach (KeyValuePair<string, string> copy in emailSettings.Copies)
                {
                    message.Cc.Add(new MailboxAddress(copy.Value, copy.Key));
                }
            }

            message.Subject = emailSettings.Subject;

            // Тело письма (в HTML и plain text)
            var bodyBuilder = new BodyBuilder
            {
                TextBody = emailSettings.TextBody,
                HtmlBody = emailSettings.HtmlBody
            };

            if(emailSettings.Attachments != null || emailSettings.Attachments?.Count != 0)
            {
                foreach (KeyValuePair<string, byte[]> attachment in emailSettings.Attachments)
                {
                    bodyBuilder.Attachments.Add(attachment.Key, attachment.Value);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var smtpClient = new SmtpClient();

                // Подключение к серверу с шифрованием STARTTLS
                await smtpClient.ConnectAsync(emailSettings.Server, emailSettings.Port, SecureSocketOptions.Auto);

                // Аутентификация (если требуется)
                //await smtpClient.AuthenticateAsync(smtpUser, smtpPassword);

                // Отправка письма
                await smtpClient.SendAsync(message);

                // Отключение
                await smtpClient.DisconnectAsync(true);

                return Result.SuccessWithMessage("Сообщение успешно отправлено.");
            }
            catch (Exception ex)
            {
                string err = "Произвошла ошибка в процессе отправки почты.";
                logger.LogError(ex, err);
                return Result.Error(err);
            }
        }

        /// <summary>
        /// Проверяем данные отправки на корректность заполнения.
        /// </summary>
        /// <param name="emailSettings">Данные отправки.</param>
        /// <returns></returns>
        private static string[] ValidateEmail(EmailSettings emailSettings)
        {
            List<string> errors = [];

            if(string.IsNullOrEmpty(emailSettings.SenderName))
                errors.Add("Отправитель не указан");

            if (string.IsNullOrEmpty(emailSettings.SenderEmail))
                errors.Add("Почта отправителя не указана");

            if (emailSettings.Recepients == null || emailSettings.Recepients.Count == 0)
                errors.Add("Адресаты не указаны");

            if (string.IsNullOrEmpty(emailSettings.Subject))
                errors.Add("Тема письма не указана");

            return [.. errors];
        }
    }
}
