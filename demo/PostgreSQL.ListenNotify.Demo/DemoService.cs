using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PostgreSQL.ListenNotify.Demo
{
    public class DemoService : IDisposable
    {
        private readonly IPostgresNotificationService _notificationService;
        private readonly ILogger<DemoService>? _logger;
        private Timer? _timer;
        private IDisposable? _notificationSubscription;
        private bool _isStarted;
        private bool _disposed;

        public DemoService(IPostgresNotificationService notificationService, ILogger<DemoService>? logger = null)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private void HandleNotification(string payload)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] Received notification: {payload}");
            Console.ResetColor();
        }

        public void Start()
        {
            if (_isStarted) return;

            Console.WriteLine("Demo service started. Will send messages periodically.");
            
            // Subscribe to the default channel using the extension method
            _notificationSubscription = _notificationService.Subscribe(
                "test_channel",
                HandleNotification);

            // Send notifications periodically
            _timer = new Timer(SendNotification, null,  TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
            
            _isStarted = true;
        }

        private async void SendNotification(object? state)
        {
            try
            {
                await _notificationService.NotifyAsync("test_channel", $"Test message at {DateTime.Now}");
                Console.WriteLine("Notification sent");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending notification: {ex.Message}");
                Console.ResetColor();
            }
        }

        public void Stop()
        {
            if (!_isStarted) return;
            
            Console.WriteLine("Demo service stopping...");

            // Clean up timer
            _timer?.Dispose();
            _timer = null;

            // Dispose the subscription
            _notificationSubscription?.Dispose();
            _notificationSubscription = null;
            
            _isStarted = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                Stop();
            }
            
            _disposed = true;
        }
    }
}
