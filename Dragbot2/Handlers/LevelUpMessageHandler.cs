using System.Text.RegularExpressions;
using Dragbot2.Resources.AppSettings;
using Dragbot2.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Dragbot2.Handlers;

public class LevelUpMessageHandler(
    ILogger<LevelUpMessageHandler> logger,
    IOptions<LevelUpSettings> optLevelUpSettings,
    GuildUserService guildUserService
) : IMessageCreateGatewayHandler
{
    private LevelUpSettings LevelUpSettings => optLevelUpSettings.Value;

    public async ValueTask HandleAsync(Message message)
    {
        if (!LevelUpSettings.Enabled) return;
        if (
            LevelUpSettings.AllowedChannelIds.Count != 0
            && !LevelUpSettings.AllowedChannelIds.Contains(message.ChannelId)
        )
            return;
        if (LevelUpSettings.AllowedUserIds.Count == 0) return;
        if (!LevelUpSettings.AllowedUserIds.Contains(message.Author.Id)) return;

        Match match = Regex.Match(message.Content, LevelUpSettings.LevelUpRegex);
        if (!match.Success) return;
        logger.LogInformation("Received level up message, content: {content}", message.Content);

        var userIdParseSuccess =
            ulong.TryParse(
                match.Groups[LevelUpSettings.UserIdRegexCaptureGroupIndex + 1].Value,
                out ulong userId
            );
        var levelParseSuccess =
            int.TryParse(
                match.Groups[LevelUpSettings.LevelRegexCaptureGroupIndex + 1].Value,
                out int level
            );

        switch (userIdParseSuccess, levelParseSuccess)
        {
            case (false, false):
                logger.LogError("Could not parse user ID and level from level up message: {message}", message);
                return;
            case (false, true):
                logger.LogError("Could not parse user ID from level up message: {message}", message);
                return;
            case (true, false):
                logger.LogError("Could not parse level from level up message: {message}", message);
                return;
        }

        if (message.GuildId == null)
        {
            logger.LogError("Could not find guild ID on message \"{message}\" with ID: {messageId}", message.Content, message.Id);
            return;
        }

        var guildId = message.GuildId.Value;
        var guildUser = await guildUserService.GetGuildUser(guildId, userId);

        await guildUserService.AddRolesToGuildUser(
            guildId,
            userId,
            LevelUpSettings.LevelRoleRequirements
                .Where(roleRequirement => roleRequirement.Value <= level)
                .Where(roleRequirement => !guildUser.RoleIds.Contains(roleRequirement.Key))
                .Select(roleRequirement => roleRequirement.Key)
                .ToList()
        );
    }
}
