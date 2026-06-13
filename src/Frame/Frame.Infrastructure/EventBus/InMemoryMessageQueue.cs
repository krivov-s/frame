using Frame.App.EventBus.Core;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Frame.Infrastructure.EventBus
{
    public class InMemoryMessageQueue<TEvent> : IEventQueue<TEvent> where TEvent : IAppNotification
    {
        private readonly Channel<TEvent> _channel;
        private readonly ILogger<InMemoryMessageQueue<TEvent>> _logger;

        public InMemoryMessageQueue(ILogger<InMemoryMessageQueue<TEvent>> logger, int capacity = 1000)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<TEvent>(options);
            _logger = logger;
        }

        public async Task WriteAsync(TEvent @event, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(@event, cancellationToken);
        }

        public async Task<Result<TEvent>> ReadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                TEvent message = await _channel.Reader.ReadAsync(cancellationToken);
                return Result<TEvent>.Success(message);
            }
            catch (OperationCanceledException)
            {
                string strMessage = "Операция была отменена";
                _logger.LogInformation("{msg}", strMessage);
                return Result<TEvent>.SuccessWithMessage(default, strMessage);
            }
            catch (ChannelClosedException)
            {
                string strMessage = "Канал был закрыт";
                _logger.LogError("{msg}", strMessage);
                return Result<TEvent>.Error(strMessage);
            }
            catch (Exception ex)
            {
                string strMessage = $"Ошибка в канале: {ex}";
                _logger.LogError(ex, "Ошибка в канале: {msg}", ex.Message);
                return Result<TEvent>.Error(strMessage);
            }
        }
    }
}
