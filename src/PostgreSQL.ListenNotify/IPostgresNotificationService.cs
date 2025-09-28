using System;
using System.Threading;
using System.Threading.Tasks;

namespace PostgreSQL.ListenNotify
{
    /// <summary>
    /// Service for interacting with PostgreSQL LISTEN/NOTIFY functionality
    /// </summary>
    public interface IPostgresNotificationService
    {
        /// <summary>
        /// Event triggered when a notification is received
        /// </summary>
        event EventHandler<PostgresNotificationEventArgs> NotificationReceived;
        
        /// <summary>
        /// Gets whether the service is currently connected to PostgreSQL
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Sends a notification to the specified channel
        /// </summary>
        /// <param name="channel">The channel to send the notification to</param>
        /// <param name="payload">The notification payload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task NotifyAsync(string channel, string payload, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sends a notification to the default channel
        /// </summary>
        /// <param name="payload">The notification payload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task NotifyAsync(string payload, CancellationToken cancellationToken = default);
    }
}