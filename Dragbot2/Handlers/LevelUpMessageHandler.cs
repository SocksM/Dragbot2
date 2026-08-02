using System.Text.RegularExpressions;
using Dragbot2.Resources.AppSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Dragbot2.Handlers;

public class LevelUpMessageHandler(ILogger<LevelUpMessageHandler> logger, IOptions<LevelUpSettings> optLevelUpSettings, GatewayClient client) : IMessageCreateGatewayHandler
{
    private LevelUpSettings LevelUpSettings => optLevelUpSettings.Value;

    public async ValueTask HandleAsync(Message message)
    {
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
        var guildUser = await client.Rest.GetGuildUserAsync(guildId, userId);

        List<Task> addRoleTasks = [];
        addRoleTasks.AddRange(
            from roleRequirement in LevelUpSettings.LevelRoleRequirements
            where roleRequirement.Key <= level
            where !guildUser.RoleIds.Contains(roleRequirement.Value)
            select client.Rest.AddGuildUserRoleAsync(guildId, guildUser.Id, roleRequirement.Value)
        );
        await Task.WhenAll(addRoleTasks);
        logger.LogInformation("Gave user {userId}, {roleCount} roles", userId, addRoleTasks.Count);
    }
}