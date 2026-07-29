using System.Threading.Channels;

namespace AILA.Infrastructure.Services.Email
{
    /// <summary>
    /// Hàng đợi email dựa trên <see cref="Channel{T}"/>, dung lượng có giới hạn.
    /// Khi đầy thì bỏ bản ghi cũ nhất thay vì để người dùng phải chờ — email OTP có TTL 5 phút,
    /// một mail tồn đọng lâu trong hàng đợi cũng đã vô dụng.
    /// </summary>
    public sealed class ChannelEmailQueue : IEmailQueue
    {
        private readonly Channel<EmailMessage> _channel;

        public ChannelEmailQueue(int capacity = 1000)
        {
            _channel = Channel.CreateBounded<EmailMessage>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });
        }

        public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            // DropOldest ⇒ TryWrite luôn thành công, nên đây thực sự là non-blocking.
            _channel.Writer.TryWrite(message);
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
