using Frame.Shared;

namespace Frame.App.EventBus.Core
{
    /// <summary>
    /// Диспетчер сообщений. Рассылает сообщения в соответствующие очереди: или непосредственно в MediatR
    /// (<see cref="PublishAsync"/> и <see cref="SendAsync"/>) или в асинхронную очередь,
    /// из которой потом специальный обработчик <see cref="EventQueueProcessor"/> сообщения заберет
    /// и отправит через <see cref="PublishAsync"/> или <see cref="SendAsync"/>. 
    /// </summary>
    public interface IAppEventDispatcher
    {
        public void CollectToSend(IAppRequest? request);
        public void CollectToQueue(IAppNotification? notification);
        public Task<Result> DispatchAllCollectedAsync();
        public void ForgetAllCollected();

        /// <summary>
        /// Начать сборку сообщений для очереди Queue. С этого момента вызовы <see cref="QueueAsync"/> будут
        /// запоминать сообщения для последующей отправки в момент вызова <see cref="CommitTransaction"/>
        /// </summary>
        public void StartTransaction();

        /// <summary>
        /// Завершить сборку сообщений и отправить в очередь все накопленные путем последовательного
        /// вызова метода <see cref="QueueAsync"/> 
        /// </summary>
        public Task CommitTransaction();
        
        /// <summary>
        /// Завершить сборку сообщений и удалить все накопленные без отправки в очередь  
        /// </summary>
        public void RollbackTransaction();
        
        /// <summary>
        /// Отправка сообщения <see cref="IAppRequest"/> через MediatR с ожиданием возврата (по сути - прямой вызов)
        /// </summary>
        /// <param name="message"></param>
        public Task<Result> SendAsync(IAppRequest? message);
        
        /// <summary>
        /// Отправка сообщения <see cref="IAppNotification"/> через MediatR с ожиданием возврата (по сути - прямой вызов)
        /// </summary>
        /// <param name="notification"></param>
        public Task<Result> PublishAsync(IAppNotification? notification);
        
        /// <summary>
        /// Отправка сообщения в очередь асинхронно (очередь может быть даже внешняя, например - RabbitMQ,
        /// определеятся реализацией в Infrastructure). Термин асинхронно здесь обозначает не async/await,
        /// а реальный межсервисный асинхронный обмен сообщениями.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public Task<Result> QueueAsync(IAppNotification? notification);
    }
}
