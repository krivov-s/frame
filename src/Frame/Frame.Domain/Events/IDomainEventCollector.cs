
namespace Frame.Domain.Events
{
    /// <summary>
    /// Коллекционирование сообщений для последующей отправки. 
    /// <para><strong>Вообще необходимость этого интерфейса, и вообще возможности выплюнуть сообщение непосредственно из Domain - под вопросом.</strong></para>
    /// Отправкой будет заниматься некто из Application, кто собственно выполняет действие над объектами в слое Domain, 
    /// и чьи действия послужили причиной формирования событий. 
    /// <para>
    /// Принятие решения об отправке будут зависеть от результата действия.
    /// Например, если проведение финансовой транзакции завершилось ошибкой, возможно эти события не будут отправлены никуда. 
    /// Это будет решать сервис проведения финансовой транзакции.
    /// </para>
    /// <para>
    /// Если объекту на уровне домена обязательно нужно оповестить кого-то о чем-то, он должен позвать нужный метод напрямую, самостоятельно.
    /// </para>
    /// </summary>
    public interface IDomainEventCollector
    {
        /// <summary>
        /// Приготовить для синхронной рассылки получателям
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        public void CollectToSend<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
        /// <summary>
        /// Приготовить для отправки в асинхронную очередь сообщений (возможно - во внешниюю, зависит от реализации на уровне Infrastructure)
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        public void CollectToQueue<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
    }
}
