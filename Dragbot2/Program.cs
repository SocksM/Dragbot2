using Dragbot2.Resources.AppSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
var appSettings = appSettingsSection.Get<AppSettings>();
if (appSettings == null) throw new Exception("AppSettings not found");
builder.Services.Configure<AppSettings>(appSettingsSection);
builder.Services.Configure<LevelUpSettings>(appSettingsSection.GetSection(nameof(AppSettings.LevelUpSettings)));
builder.Services.Configure<DiscordSettings>(appSettingsSection.GetSection(nameof(AppSettings.DiscordSettings)));

builder.Services
    .AddDiscordGateway(settings =>
    {
        settings.Token = appSettings.DiscordSettings.BotToken;
        settings.Intents =
            GatewayIntents.MessageContent |
            GatewayIntents.GuildMessages |
            0;
    })
    .AddGatewayHandlers(typeof(Program).Assembly);


var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Bot starting...");

await app.RunAsync();