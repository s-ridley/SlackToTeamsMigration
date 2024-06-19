using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SlackToTeams.Services;

static IHost AppStartup() {
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("settings/appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("settings/logging.json", optional: false, reloadOnChange: true)
        .AddJsonFile("settings/team.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    var configuration = builder.Build();

    Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

    Log.Logger.Information("Starting - SlackToTeams");

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) => {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IMigrationService, MigrationService>();
        })
        .UseSerilog()
        .Build();

    return host;
}

var host = AppStartup();

var migrationService = ActivatorUtilities.CreateInstance<MigrationService>(host.Services);

Console.CancelKeyPress += (sender, eventArgs) => {
    // Cancel the event and call the close process
    eventArgs.Cancel = true;

    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine();
    Console.WriteLine("===========================================");
    Console.WriteLine("      Ctrl+C detected. Cleaning up...      ");
    Console.WriteLine("===========================================");
    Console.WriteLine();
    Console.ResetColor();

    migrationService.StopAsync();
};

await migrationService.StartAsync();
