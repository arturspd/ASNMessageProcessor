using ASNMessageProcessor.Context;
using ASNMessageProcessor.Models.Configuration;
using ASNMessageProcessor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<FileWatcherSettings>(context.Configuration.GetSection("FileWatcherSettings"));
        services.AddHostedService<FileWatcherService>();
        services.AddScoped<IFileParser, FileParser>();
        services.AddScoped<IDataInserter, DataInserter>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(context.Configuration.GetConnectionString("SqliteConnection"));
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();