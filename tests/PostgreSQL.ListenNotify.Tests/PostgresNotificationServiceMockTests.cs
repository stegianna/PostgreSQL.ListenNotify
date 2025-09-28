using Microsoft.Extensions.Logging;
using Moq;
using PostgreSQL.ListenNotify.Configuration;

namespace PostgreSQL.ListenNotify.Tests
{
    [TestFixture]
    public class PostgresNotificationServiceMockTests
    {
        private Mock<ILogger<PostgresNotificationService>> _loggerMock;
        private PostgresNotificationOptions _options;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PostgresNotificationService>>();
            _options = new PostgresNotificationOptions
            {
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password",
                ListenChannels = new List<string> { "test_channel" },
                DefaultNotifyChannel = "default_channel"
            };
        }

        [Test]
        public void EventHandler_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            var service = new PostgresNotificationService(_loggerMock.Object, _options);
            
            // Act & Assert - Subscribe
            EventHandler<PostgresNotificationEventArgs> handler = (sender, args) => { };
            service.NotificationReceived += handler;
            
            // Act & Assert - Unsubscribe
            service.NotificationReceived -= handler;
            
            // Test passes if no exceptions are thrown
            Assert.Pass();
        }

        [Test]
        public async Task StopAsync_DisposesConnections()
        {
            // Arrange
            var service = new PostgresNotificationService(_loggerMock.Object, _options);
            
            // Act
            await service.StopAsync(CancellationToken.None);
            
            // Assert - Since we can't easily verify internal state directly,
            // we're mainly verifying the method completes without exceptions
            Assert.Pass();
        }
    }
}