using System;
using System.Collections.Generic;

namespace PostgreSQL.ListenNotify.Configuration
{
    /// <summary>
    /// Configuration options for PostgreSQL Listen/Notify service
    /// </summary>
    public class PostgresNotificationOptions
    {
        /// <summary>
        /// The PostgreSQL connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Channels to listen for notifications
        /// </summary>
        public List<string> ListenChannels { get; set; } = new List<string>();

        /// <summary>
        /// Default channel for sending notifications
        /// </summary>
        public string DefaultNotifyChannel { get; set; }

        /// <summary>
        /// Application name to use in PostgreSQL connection
        /// </summary>
        public string ApplicationName { get; set; } = "PostgreSQL.ListenNotify";

        /// <summary>
        /// Time in seconds to wait before attempting to reconnect after failure
        /// </summary>
        public int ReconnectDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Validates that all required options are properly set
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when required options are missing</exception>
        /// <exception cref="ArgumentException">Thrown when options are invalid</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString), "PostgreSQL connection string is required");

            if (ListenChannels.Count == 0)
                throw new ArgumentException("At least one channel to listen to must be specified", nameof(ListenChannels));
        }
    }
}