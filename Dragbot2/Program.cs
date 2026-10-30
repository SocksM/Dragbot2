using Dragbot2.Caches;
using Dragbot2.Commands.CosmeticSelectionRoles;
using Dragbot2.Resources.AppSettings;
using Dragbot2.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
var appSettings = appSettingsSection.Get<AppSettings>();
if (appSettings == null) throw new Exception("AppSettings not found");
var levelUpSetting = appSettings.LevelUpSettings;
var discordSettings = appSettings.DiscordSettings;
var cosmeticRolesSettings = appSettings.CosmeticRolesSettings;
builder.Services.Configure<AppSettings>(appSettingsSection);
builder.Services.Configure<LevelUpSettings>(appSettingsSection.GetSection(nameof(AppSettings.LevelUpSettings)));
builder.Services.Configure<DiscordSettings>(appSettingsSection.GetSection(nameof(AppSettings.DiscordSettings)));
builder.Services.Configure<CosmeticRolesSettings>(appSettingsSection.GetSection(nameof(AppSettings.CosmeticRolesSettings)));

builder.Services.AddDiscordGateway(settings =>
{
    settings.Token = appSettings.DiscordSettings.BotToken;
    settings.Intents =
        GatewayIntents.MessageContent |
        GatewayIntents.GuildMessages |
        GatewayIntents.DirectMessages |
        0;
});

builder.Services.AddGatewayHandlers(typeof(Program).Assembly);

builder.Services.AddApplicationCommands();

builder.Services.AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>();
builder.Services.AddComponentInteractions<StringMenuInteraction, StringMenuInteractionContext>();
builder.Services.AddComponentInteractions<UserMenuInteraction, UserMenuInteractionContext>();
builder.Services.AddComponentInteractions<RoleMenuInteraction, RoleMenuInteractionContext>();
builder.Services.AddComponentInteractions<MentionableMenuInteraction, MentionableMenuInteractionContext>();
builder.Services.AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>();
builder.Services.AddComponentInteractions<ModalInteraction, ModalInteractionContext>();

builder.Services.AddScoped<IMemoryCache, MemoryCache>();
builder.Services.AddScoped<RoleCache>();
builder.Services.AddScoped<GuildUserCache>();
builder.Services.AddScoped<GuildUserService>();
builder.Services.AddScoped<RoleService>();

var app = builder.Build();

if (cosmeticRolesSettings.Enabled)
    app.AddModules(typeof(CosmeticSelectionRolesCommands).Assembly);

await app.RunAsync();
