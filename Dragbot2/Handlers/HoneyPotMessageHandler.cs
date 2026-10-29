using Dragbot2.Resources;
using Dragbot2.Resources.AppSettings;
using Dragbot2.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Dragbot2.Handlers;

public class HoneyPotMessageHandler(
    ILogger<HoneyPotMessageHandler> logger,
    IOptions<HoneypotChannelSettings> optHoneypotChannelSettings,
    GatewayClient client,
    GuildUserService guildUserService
) : IMessageCreateGatewayHandler
{
    private HoneypotChannelSettings HoneypotChannelSettings => optHoneypotChannelSettings.Value;

    public async ValueTask HandleAsync(Message message)
    {
        if (!HoneypotChannelSettings.Enabled) return;
        if (!HoneypotChannelSettings.HoneyPotChannelIds.Contains(message.ChannelId)) return;

        if (HoneypotChannelSettings.HoneypotAction == HoneypotAction.None)
        {
            logger.LogInformation("Honeypot was triggered but no action was configured, deleting message from honeypot channel and bailing.");
            await message.DeleteAsync();
            return;
        }

        if (message.GuildId == null)
        {
            logger.LogError("Could not find guild ID on honeypot message {messageId}", message.Id);
            return;
        }

        List<Task> actions = [message.DeleteAsync()];

        var senderGuildUser = await guildUserService.GetGuildUser(message.GuildId.Value, message.Author.Id);

        if (HoneypotChannelSettings.HoneypotAction.HasFlag(HoneypotAction.Ban))
            actions.Add(senderGuildUser.BanAsync());
        else if (HoneypotChannelSettings.HoneypotAction.HasFlag(HoneypotAction.Kick))
            actions.Add(senderGuildUser.KickAsync());
        else if (HoneypotChannelSettings.HoneypotAction.HasFlag(HoneypotAction.Timeout))
            if (HoneypotChannelSettings.TimeoutMinutes.HasValue)
                actions.Add(senderGuildUser.TimeOutAsync(DateTime.UtcNow.AddMinutes(HoneypotChannelSettings.TimeoutMinutes.Value)));
            else
                logger.LogWarning("{HoneypotChannelSettings}.{TimeoutMinutes} was null but the action was configured to timeout the user",
                    nameof(AppSettings.HoneypotChannelSettings),
                    nameof(Resources.AppSettings.HoneypotChannelSettings.TimeoutMinutes)
                );
        if (HoneypotChannelSettings.HoneypotAction.HasFlag(HoneypotAction.DeleteRecentMessages))
            if (HoneypotChannelSettings.DeleteMessagesMinutes.HasValue)
                actions.Add(Task.Run(async () =>
                {
                    // delay incase the user sends more messages at the same time, and they aren't picked up (assuming it's a bot sending the message).
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await DeleteRecentMessagesAsync(message.ChannelId, message.Author.Id, message.Id, HoneypotChannelSettings.DeleteMessagesMinutes.Value);
                }));
            else
                logger.LogWarning("{HoneypotChannelSettings}.{DeleteMessagesMinutes} was null but the action was configured to delete recently sent messages by the user",
                    nameof(AppSettings.HoneypotChannelSettings),
                    nameof(Resources.AppSettings.HoneypotChannelSettings.DeleteMessagesMinutes)
                );

        await Task.WhenAll(actions);

        logger.LogInformation("Preformed the honeypot actions on user {userId} that sent message {messageId}, in guild {guildId}", message.Author.Id, message.Id, message.GuildId.Value);
    }

    private async Task DeleteRecentMessagesAsync(
        ulong channelId,
        ulong userId,
        ulong triggeringMessageId,
        int minutes)
    {
        if (minutes <= 0)
            return;

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-minutes);

        await foreach (var recentMessage in client.Rest.GetMessagesAsync(channelId))
        {
            // Messages are fetched newest first, so there is no need to continue past the cutoff.
            if (recentMessage.CreatedAt < cutoff)
                break;

            if (recentMessage.Author.Id != userId || recentMessage.Id == triggeringMessageId)
                continue;

            await client.Rest.DeleteMessageAsync(channelId, recentMessage.Id);
        }
    }
}
