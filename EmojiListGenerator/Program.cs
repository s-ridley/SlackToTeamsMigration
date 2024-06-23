using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EmojiListGenerator.Services;

static IHost AppStartup() {
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("settings/appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    var configuration = builder.Build();

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) => {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IGeneratorService, GeneratorService>();
        })
        .Build();

    return host;
}

var host = AppStartup();

var generatorService = ActivatorUtilities.CreateInstance<GeneratorService>(host.Services);

await generatorService.RunAsync();
