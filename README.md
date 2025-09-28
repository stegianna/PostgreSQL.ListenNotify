# PostgreSQL.ListenNotify

A .NET library that provides a clean and robust implementation of PostgreSQL's LISTEN/NOTIFY functionality for .NET applications. This library enables real-time notifications between applications using PostgreSQL as a message broker.

## Features

- Easy integration with .NET Dependency Injection
- Automatic connection management with reconnection capability
- Strongly-typed notification events
- Async/await pattern support
- Multiple channel subscription
- Application-friendly event args

## Installation

Install the package via NuGet: `dotnet add package PostgreSQL.ListenNotify` 

or via the NuGet Package Manager Console: `Install-Package PostgreSQL.ListenNotify`


## Quick Start

### 1. Register the service in your application
```csharp
// In Program.cs or Startup.cs 

services.AddPostgresNotifications(options => 
{
    options.ConnectionString = "Host=localhost;Database=yourdb;Username=user;Password=pass"; 
    options.ListenChannels = new() { "channel1", "channel2" };
    options.DefaultNotifyChannel = "channel1";
    options.ApplicationName = "YourAppName";
});
```


### 2. Use the notification service in your code
```csharp
public class NotificationHandler : IDisposable
{ 
    private readonly IPostgresNotificationService _notificationService;
    private IDisposable? _subscription;

    public NotificationHandler(IPostgresNotificationService notificationService)
    {
        _notificationService = notificationService;
        
        // Subscribe to notifications using the Subscribe method
        _subscription = _notificationService.Subscribe(OnNotificationReceived);
    }

    private void OnNotificationReceived(PostgresNotificationEventArgs e)
    {
        Console.WriteLine($"Received on {e.Channel}: {e.Payload}");
        // Process the notification here
    }

    // Alternative approach with inline handler
    public void SubscribeToSpecificChannel()
    {
        // Subscribe to a specific channel with inline handler
        _notificationService.Subscribe("custom_channel", notification => 
        {
            Console.WriteLine($"Specific handler for {notification.Channel}: {notification.Payload}");
        });
    }

    public async Task SendNotificationsAsync()
    {
        // Send a notification to the default channel
        await _notificationService.NotifyAsync("This is a notification message");
        
        // Send a notification to a specific channel
        await _notificationService.NotifyAsync("custom_channel", "This is a message for a specific channel");
    }
    
    // Clean and simple cleanup with IDisposable pattern
    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
```

Registrer the `NotificationHandler` in Program.cs or Startup.cs as well:
```csharp
services.AddSingleton<NotificationHandler>();
```

Ensure that the `NotificationHandler` is instantiated at application startup to begin listening for notifications.

## Configuration Options

The `PostgresNotificationOptions` class provides the following configuration options:

| Property | Description | Default |
|----------|-------------|---------|
| ConnectionString | Required PostgreSQL connection string | null |
| ListenChannels | List of channels to listen for notifications | Empty list |
| DefaultNotifyChannel | Default channel for sending notifications | null |
| ApplicationName | Application name to identify connections in PostgreSQL | "PostgreSQL.ListenNotify" |
| ReconnectDelaySeconds | Time to wait before reconnecting after failure | 5 |

## How It Works

The library creates two separate connections to PostgreSQL:
1. A dedicated listener connection that stays open and waits for notifications
2. A connection for sending notifications

### Connection Management and Event Handling

- **Automatic Reconnection**: The library automatically handles connection issues by implementing a robust reconnection mechanism. If the PostgreSQL connection is lost, the library will attempt to reconnect.
- **Connection State Events**: You can monitor connection state changes through events, allowing your application to react appropriately when connections are established, lost, or reconnected.
- **Rich Event Arguments**: The `PostgresNotificationEventArgs` provides comprehensive information about each notification including:
  - Channel name
  - Payload (message content)
  - Timestamp
  - Process ID of the sender

### Channel Subscription

- **Dynamic Channel Subscription**: Beyond the initial configuration, you can programmatically subscribe to additional channels at runtime.
- **Multiple Subscription Methods**:
  - Subscribe to all configured channels with a single handler
  - Subscribe to specific channels with dedicated handlers
  - Use strongly-typed event pattern or delegate-based subscriptions

### Integration Features

- Seamless integration with dependency injection systems
- Works well in various application types including web applications, services, and desktop applications
- Designed to work reliably in containerized environments