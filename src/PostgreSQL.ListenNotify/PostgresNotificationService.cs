using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using PostgreSQL.ListenNotify.Configuration;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PostgreSQL.ListenNotify
{
    /// <summary>
    /// Background service that implements PostgreSQL LISTEN/NOTIFY functionality
    /// </summary>
    public class PostgresNotificationService : BackgroundService, IPostgresNotificationService
    {
        private readonly ILogger<PostgresNotificationService> _logger;
        private readonly PostgresNotificationOptions _options;
        private readonly string _connectionString;
        private NpgsqlConnection _connection;
        private NpgsqlConnection _notifyConnection;
        private bool _reconnecting;
        private readonly TimeSpan _reconnectDelay;

        /// <summary>
        /// Event triggered when a notification is received
        /// </summary>
        public event EventHandler<PostgresNotificationEventArgs> NotificationReceived;

        /// <summary>
        /// Gets whether the service is currently connected to PostgreSQL
        /// </summary>
        public bool IsConnected => _connection?.State == ConnectionState.Open;

        /// <summary>
        /// Creates a new instance of PostgresNotificationService
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="options">Configuration options</param>
        public PostgresNotificationService(
            ILogger<PostgresNotificationService> logger,
            PostgresNotificationOptions options)
        {
            _logger = logger;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            
            // Validate options
            _options.Validate();
            
            // Create connection string with pooling disabled for notification connections
            var connStringBuilder = new NpgsqlConnectionStringBuilder(_options.ConnectionString)
            {
                Pooling = false, // Disable pooling for notification connections
                ApplicationName = _options.ApplicationName // Identify this connection in logs
            };
            
            _connectionString = connStringBuilder.ToString();
            _reconnectDelay = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds);
        }

        /// <summary>
        /// Executes the background service
        /// </summary>
        /// <param name="stoppingToken">Cancellation token</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostgreSQL Notification Service starting at: {time}", DateTimeOffset.Now);

            await ConnectAndListenAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Keep the service alive while connection is handled by the notification thread
                await Task.Delay(1000, stoppingToken);

                // Try to reconnect if we've lost connection and aren't already reconnecting
                if (_connection?.State != ConnectionState.Open && !_reconnecting)
                {
                    _logger.LogWarning("Connection lost. Attempting to reconnect...");
                    await ConnectAndListenAsync(stoppingToken);
                }
            }
        }

        private async Task ConnectAndListenAsync(CancellationToken stoppingToken)
        {
            try
            {
                _reconnecting = true;

                // Clean up any existing connections
                await CleanupConnectionsAsync();

                // Create a new connection with pooling disabled for listening
                _connection = new NpgsqlConnection(_connectionString);

                // Register event handlers
                _connection.Notification += OnPostgresNotificationReceived;
                _connection.StateChange += OnConnectionStateChange;

                // Open connection
                await _connection.OpenAsync(stoppingToken);
                _logger.LogInformation("Connected to PostgreSQL successfully for listening");

                // Listen to all configured channels
                using var command = _connection.CreateCommand();
                foreach (var channel in _options.ListenChannels)
                {
                    command.CommandText = $"LISTEN {channel};";
                    await command.ExecuteNonQueryAsync(stoppingToken);
                    _logger.LogInformation("Now listening on channel: {channel}", channel);
                }

                // Create separate connection for notifications
                _notifyConnection = new NpgsqlConnection(_connectionString);
                await _notifyConnection.OpenAsync(stoppingToken);
                _logger.LogInformation("Connected to PostgreSQL successfully for notifications");

                // Start waiting for notifications asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (_connection?.State == ConnectionState.Open && !stoppingToken.IsCancellationRequested)
                        {
                            // Wait for notifications (this is a blocking call that waits for notifications)
                            await _connection.WaitAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "Error while waiting for notifications");
                    }
                }, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to connect to PostgreSQL");

                // Schedule reconnection attempt
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_reconnectDelay, stoppingToken);
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await ConnectAndListenAsync(stoppingToken);
                    }
                }, stoppingToken);
            }
            finally
            {
                _reconnecting = false;
            }
        }

        private void OnPostgresNotificationReceived(object sender, NpgsqlNotificationEventArgs e)
        {
            _logger.LogInformation("Received notification on channel {Channel}: {Payload}", e.Channel, e.Payload);
            
            // Convert Npgsql notification to our custom event args and raise event
            NotificationReceived?.Invoke(this, 
                new PostgresNotificationEventArgs(e.Channel, e.Payload, e.PID));
        }

        private void OnConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            _logger.LogInformation("PostgreSQL connection state changed from {OldState} to {NewState}",
                e.OriginalState, e.CurrentState);
        }

        /// <summary>
        /// Sends a notification to the specified channel
        /// </summary>
        /// <param name="channel">The channel to send the notification to</param>
        /// <param name="payload">The notification payload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task NotifyAsync(string channel, string payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException(nameof(channel));

            if (_notifyConnection?.State != ConnectionState.Open)
            {
                _logger.LogWarning("Notification connection not open. Attempting to open...");
                try
                {
                    if (_notifyConnection == null)
                    {
                        _notifyConnection = new NpgsqlConnection(_connectionString);
                    }
                    await _notifyConnection.OpenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to open notification connection");
                    throw;
                }
            }

            try
            {
                using var command = _notifyConnection.CreateCommand();
                // Properly escape payload to prevent SQL injection
                command.CommandText = $"SELECT pg_notify(@channel, @payload)";
                command.Parameters.AddWithValue("payload", payload ?? string.Empty);
                command.Parameters.AddWithValue("channel", channel);
                await command.ExecuteNonQueryAsync(cancellationToken);
                
                _logger.LogInformation("Sent notification to channel {Channel}: {Payload}", channel, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to channel {Channel}", channel);
                throw;
            }
        }

        /// <summary>
        /// Sends a notification to the default channel
        /// </summary>
        /// <param name="payload">The notification payload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task NotifyAsync(string payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_options.DefaultNotifyChannel))
                throw new InvalidOperationException("DefaultNotifyChannel is not configured");
                
            return NotifyAsync(_options.DefaultNotifyChannel, payload, cancellationToken);
        }

        /// <summary>
        /// Stops the service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PostgreSQL Notification Service stopping");
            await CleanupConnectionsAsync();
            await base.StopAsync(cancellationToken);
        }

        private async Task CleanupConnectionsAsync()
        {
            // Clean up listener connection
            if (_connection != null)
            {
                _connection.Notification -= OnPostgresNotificationReceived;
                _connection.StateChange -= OnConnectionStateChange;

                if (_connection.State == ConnectionState.Open)
                {
                    try
                    {
                        using var command = _connection.CreateCommand();
                        foreach (var channel in _options.ListenChannels)
                        {
                            command.CommandText = $"UNLISTEN {channel};";
                            await command.ExecuteNonQueryAsync();
                            _logger.LogInformation("Unsubscribed from channel: {channel}", channel);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while unlistening from channels");
                    }

                    await _connection.CloseAsync();
                }

                await _connection.DisposeAsync();
                _connection = null;
            }

            // Clean up notifier connection
            if (_notifyConnection != null)
            {
                if (_notifyConnection.State == ConnectionState.Open)
                {
                    await _notifyConnection.CloseAsync();
                }

                await _notifyConnection.DisposeAsync();
                _notifyConnection = null;
            }
        }
    }
}