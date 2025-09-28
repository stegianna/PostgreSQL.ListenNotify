using System;

namespace PostgreSQL.ListenNotify
{
    /// <summary>
    /// Event arguments for PostgreSQL notifications
    /// </summary>
    public class PostgresNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// The channel the notification was received on
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// The payload of the notification
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// The process ID that sent the notification
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Creates a new instance of PostgresNotificationEventArgs
        /// </summary>
        /// <param name="channel">The notification channel</param>
        /// <param name="payload">The notification payload</param>
        /// <param name="processId">The process ID that sent the notification</param>
        public PostgresNotificationEventArgs(string channel, string payload, int processId)
        {
            Channel = channel;
            Payload = payload;
            ProcessId = processId;
        }
    }
}