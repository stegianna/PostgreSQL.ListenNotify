using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostgreSQL.ListenNotify.DependencyInjection;

namespace PostgreSQL.ListenNotify.Tests
{
    /// <summary>
    /// Base class for integration tests that require a real PostgreSQL instance.
    /// Only run these tests when you have a PostgreSQL server available.
    /// </summary>
    [TestFixture, Explicit("Requires PostgreSQL server")]
    public class IntegrationTestBase
    {
        protected IHost _host;
        protected IPostgresNotificationService _notificationService;
        protected string _connectionString = "Host=localhost;Port=5432;Username=postgressu;Password=postgrespassword;Database=postgres;";
        protected string _testChannel = "test_channel";

        [OneTimeSetUp]
        public async Task SetupIntegrationTest()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddPostgresNotifications(options =>
                    {
                        options.ConnectionString = _connectionString;
                        options.ListenChannels = new List<string> { _testChannel };
                        options.DefaultNotifyChannel = _testChannel;
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .Build();

            await _host.StartAsync();
            _notificationService = _host.Services.GetRequiredService<IPostgresNotificationService>();
            
            // Wait for service to initialize
            await Task.Delay(1000);
        }

        [OneTimeTearDown]
        public async Task TeardownIntegrationTest()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }

        [Test]
        public void IsConnected_ShouldBeTrue_AfterStartup()
        {
            // Assert
            Assert.That(_notificationService.IsConnected, Is.True, "Service should be connected after startup");
        }

        [Test]
        public async Task NotifyAsync_ShouldSendNotification()
        {
            // Arrange
            var notificationReceived = false;
            var payload = $"test-{Guid.NewGuid()}";
            var waitHandle = new TaskCompletionSource<bool>();

            // Use the Subscribe method instead of direct event handling
            using var subscription = _notificationService.Subscribe(_testChannel, receivedPayload =>
            {
                if (receivedPayload == payload)
                {
                    notificationReceived = true;
                    waitHandle.TrySetResult(true);
                }
            });

            try
            {
                // Act
                await _notificationService.NotifyAsync(_testChannel, payload);
                
                // Wait for notification with timeout
                var completed = await Task.WhenAny(waitHandle.Task, Task.Delay(5000));
                
                // Assert
                Assert.That(completed == waitHandle.Task, Is.True, "Notification should be received within timeout");
                Assert.That(notificationReceived, Is.True, "Notification should be received");
            }
            finally
            {
                // No need for manual cleanup as we're using 'using' statement
            }
        }

        [Test]
        public async Task Service_ShouldReconnect_WhenConnectionIsInterrupted()
        {
            // Arrange
            var initialConnectionState = _notificationService.IsConnected;
            Assert.That(initialConnectionState, Is.True, "Service should be connected before testing reconnection");
            
            // Get access to the underlying PostgresNotificationService to simulate connection interruption
            var serviceField = _notificationService.GetType().GetField("_connection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (serviceField == null)
            {
                Assert.Fail("Cannot access internal connection field for test");
                return;
            }
            
            // Create event monitoring for reconnection
            var reconnected = false;
            var waitHandle = new TaskCompletionSource<bool>();
            var originalConnection = serviceField.GetValue(_notificationService);
            
            // Use the Subscribe method for monitoring notifications
            using var subscription = _notificationService.Subscribe(_testChannel, _ =>
            {
                // If we receive a notification after reconnection, the test has passed
                reconnected = true;
                waitHandle.TrySetResult(true);
            });
            
            try
            {
                // Act - Force close the connection to simulate interruption
                var npgsqlConn = originalConnection as Npgsql.NpgsqlConnection;
                if (npgsqlConn != null)
                {
                    npgsqlConn.Close();
                    Console.WriteLine("Connection forcibly closed");
                }
                
                // Wait for reconnection (service checks every second plus reconnect delay)
                // We'll wait up to 10 seconds for reconnection to occur
                await Task.Delay(5000); 
                
                // Send a test notification to verify reconnection
                var testPayload = $"reconnect-test-{Guid.NewGuid()}";
                await _notificationService.NotifyAsync(_testChannel, testPayload);
                
                // Wait for notification with timeout
                var completed = await Task.WhenAny(waitHandle.Task, Task.Delay(10000));
                
                // Assert
                Assert.That(_notificationService.IsConnected, Is.True, "Service should have reconnected");
                Assert.That(completed == waitHandle.Task, Is.True, "Should receive notification after reconnection");
                Assert.That(reconnected, Is.True, "Should successfully process notifications after reconnection");
                
                // Verify the connection object was replaced (further evidence of reconnection)
                var newConnection = serviceField.GetValue(_notificationService);
                Assert.That(newConnection, Is.Not.SameAs(originalConnection), "A new connection should have been created");
            }
            finally
            {
                // No need for manual cleanup as we're using 'using' statement
            }
        }
    }
}