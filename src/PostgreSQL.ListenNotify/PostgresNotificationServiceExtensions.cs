using PostgreSQL.ListenNotify;

public static class PostgresNotificationServiceExtensions
{
    public static IDisposable Subscribe(
        this IPostgresNotificationService service,
        string channel,
        Action<string> handler)
    {
        var subscription = new PostgresNotificationSubscription(service, channel, handler);
        return subscription;
    }
    
    private class PostgresNotificationSubscription : IDisposable
    {
        private readonly IPostgresNotificationService _service;
        private readonly string _channel;
        private readonly Action<string> _handler;
        
        public PostgresNotificationSubscription(
            IPostgresNotificationService service,
            string channel,
            Action<string> handler)
        {
            _service = service;
            _channel = channel;
            _handler = handler;
            
            _service.NotificationReceived += OnNotificationReceived;
        }
        
        private void OnNotificationReceived(object? sender, PostgresNotificationEventArgs e)
        {
            if (e.Channel == _channel)
            {
                _handler(e.Payload);
            }
        }
        
        public void Dispose()
        {
            _service.NotificationReceived -= OnNotificationReceived;
        }
    }
}