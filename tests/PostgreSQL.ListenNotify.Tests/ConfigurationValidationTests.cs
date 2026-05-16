using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PostgreSQL.ListenNotify.Configuration;
using PostgreSQL.ListenNotify.DependencyInjection;

namespace PostgreSQL.ListenNotify.Tests
{
    [TestFixture, Category("Unit")]
    public class PostgresNotificationOptionsTests
    {
        [Test]
        public void Validate_WithValidConfiguration_DoesNotThrow()
        {
            // Arrange
            var options = new PostgresNotificationOptions
            {
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password",
                ListenChannels = new List<string> { "test_channel" }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => options.Validate());
        }

        [Test]
        public void Validate_WithMissingConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var options = new PostgresNotificationOptions
            {
                ConnectionString = null,
                ListenChannels = new List<string> { "test_channel" }
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.Validate());
            Assert.That(ex.ParamName, Is.EqualTo("ConnectionString"));
        }

        [Test]
        public void Validate_WithEmptyListenChannels_ThrowsArgumentException()
        {
            // Arrange
            var options = new PostgresNotificationOptions
            {
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password",
                ListenChannels = new List<string>()
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.That(ex.ParamName, Is.EqualTo("ListenChannels"));
        }
    }

    [TestFixture, Category("Unit")]
    public class PostgresNotificationEventArgsTests
    {
        [Test]
        public void Constructor_SetsProperties()
        {
            // Arrange
            const string channel = "test_channel";
            const string payload = "test_payload";
            const int processId = 12345;

            // Act
            var eventArgs = new PostgresNotificationEventArgs(channel, payload, processId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(eventArgs.Channel, Is.EqualTo(channel));
                Assert.That(eventArgs.Payload, Is.EqualTo(payload));
                Assert.That(eventArgs.ProcessId, Is.EqualTo(processId));
            });
        }
    }

    [TestFixture, Category("Unit")]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddPostgresNotifications_WithValidOptions_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddPostgresNotifications(options =>
            {
                options.ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password";
                options.ListenChannels = new List<string> { "test_channel" };
            });

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(serviceProvider.GetService<PostgresNotificationOptions>(), Is.Not.Null);
                Assert.That(serviceProvider.GetService<IHostedService>(), Is.Not.Null);
                Assert.That(serviceProvider.GetService<IPostgresNotificationService>(), Is.Not.Null);
            });
        }

        [Test]
        public void AddPostgresNotifications_WithNullConfigAction_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            Action<PostgresNotificationOptions> configAction = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddPostgresNotifications(configAction));
        }
    }

    [TestFixture, Category("Unit")]
    public class PostgresNotificationServiceTests
    {
        private Mock<ILogger<PostgresNotificationService>> _loggerMock;
        private PostgresNotificationOptions _validOptions;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PostgresNotificationService>>();
            _validOptions = new PostgresNotificationOptions
            {
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password",
                ListenChannels = new List<string> { "test_channel" },
                DefaultNotifyChannel = "test_channel",
                ApplicationName = "TestApp",
                ReconnectDelaySeconds = 1
            };
        }

        [Test]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PostgresNotificationService(_loggerMock.Object, null));
        }

        [Test]
        public void Constructor_WithValidOptions_CreatesInstance()
        {
            // Act
            var service = new PostgresNotificationService(_loggerMock.Object, _validOptions);

            // Assert
            Assert.That(service, Is.Not.Null);
            Assert.That(service.IsConnected, Is.False); // Should be false initially
        }

        [Test]
        public void NotifyAsync_WithNullOrEmptyChannel_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new PostgresNotificationService(_loggerMock.Object, _validOptions);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => service.NotifyAsync(null, "test", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentNullException>(() => service.NotifyAsync(string.Empty, "test", CancellationToken.None));
        }

        [Test]
        public void NotifyAsync_WithNoDefaultChannel_ThrowsInvalidOperationException()
        {
            // Arrange
            _validOptions.DefaultNotifyChannel = null;
            var service = new PostgresNotificationService(_loggerMock.Object, _validOptions);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => service.NotifyAsync("test", CancellationToken.None));
        }
    }
}