namespace PostgreSQL.ListenNotify.DemoWebAPI
{
    public class NotificationHandler : IDisposable
    {
        private readonly IPostgresNotificationService _notificationService;
        private readonly ILogger<NotificationHandler>? _logger;
        private IDisposable? _notificationSubscription;

        public NotificationHandler(
            IPostgresNotificationService notificationService,
            ILogger<NotificationHandler>? logger = null)
        {
            _notificationService = notificationService;
            _logger = logger;

            _notificationSubscription = _notificationService.Subscribe("test_channel", HandleNotification);
        }

        private void HandleNotification(string payload)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] Received notification: {payload}");
            Console.ResetColor();
        }

        public async void SendNotification(string payload)
        {
            try
            {
                await _notificationService.NotifyAsync("test_channel", payload);
                Console.WriteLine("Notification sent");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending notification: {ex.Message}");
                Console.ResetColor();
            }
        }
        public void Dispose()
        {
            // Clean up subscription when handler is disposed
            _notificationSubscription?.Dispose();
        }
    }
}
