using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostgreSQL.ListenNotify.DependencyInjection;

namespace PostgreSQL.ListenNotify.Demo
{
    class Program
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Username=postgressu;Password=postgrespassword;Database=postgres;";
        private const string ChannelName = "test_channel";

        static async Task Main(string[] args)
        {
            Console.WriteLine("PostgreSQL Listen/Notify Demo");
            Console.WriteLine("============================");

            // Setup Host with DI
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    // Register PostgreSQL notification service
                    services.AddPostgresNotifications(options =>
                    {
                        options.ConnectionString = ConnectionString;
                        options.ListenChannels = new() { ChannelName };
                        options.DefaultNotifyChannel = ChannelName;
                        options.ApplicationName = "PostgreSQL.ListenNotify.Demo";
                    });
                    
                    // Register as a singleton service (not hosted)
                    services.AddSingleton<DemoService>();
                })
                .Build();

            try
            {
                // Start the host services (including PostgresNotificationService)
                await host.StartAsync();
                
                Console.WriteLine("Host started successfully");
                
                // Get and start the DemoService
                var demoService = host.Services.GetRequiredService<DemoService>();
                demoService.Start();
                
                // Wait for the host to be terminated
                await host.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Application error: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }
}
