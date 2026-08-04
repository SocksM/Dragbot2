using Dragbot2.Caches;
using Dragbot2.Resources.AppSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Dragbot2.Commands.CosmeticSelectionRoles;

[SlashCommand("cosmetic-roles", "Select cosmetic roles")]
public class CosmeticSelectionRolesCommands(
    IOptions<CosmeticRolesSettings> optCosmeticRoleSettings,
    GatewayClient client,
    ILogger<CosmeticSelectionRolesCommands> logger,
    RoleCache roleCache
) : ApplicationCommandModule<ApplicationCommandContext>
{
    private CosmeticRolesSettings CosmeticRolesSettings => optCosmeticRoleSettings.Value;

    public const string BasicColorsDropdownId = "basic-colors-dropdown";

    [SubSlashCommand("basic-colors", "Select basic colors")]
    public async Task BasicColorSelection()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        if (!Context.Interaction.GuildId.HasValue)
        {
            logger.LogError("Couldn't find guildId in the context");
            await Context.Interaction.ModifyResponseAsync(msgOpts =>
            {
                msgOpts.Content = "Something went wrong, please try again later.";
                msgOpts.Flags = MessageFlags.Ephemeral;
            });
            return;
        }

        GuildUser guildUser = await client.Rest.GetGuildUserAsync(Context.Interaction.GuildId.Value, Context.User.Id);
        if (CosmeticRolesSettings.RequiredRoleIdForBasicColors != null
            && !guildUser.RoleIds.Contains(CosmeticRolesSettings.RequiredRoleIdForBasicColors.Value)
           )
        {
            await Context.Interaction.ModifyResponseAsync(msgOpts =>
            {
                msgOpts.Content = $"You do not have the required role to use this command.\nRole required: <@&{CosmeticRolesSettings.RequiredRoleIdForBasicColors.Value}>";
                msgOpts.Flags = MessageFlags.Ephemeral;
                msgOpts.AllowedMentions = new AllowedMentionsProperties
                {
                    AllowedRoles = [],
                };
            });
            return;
        }

        List<Task<Role>> getRoleTasks = new(capacity: CosmeticRolesSettings.BasicColorRoleIds.Count);
        getRoleTasks.AddRange(
            CosmeticRolesSettings.BasicColorRoleIds.Select(roleId =>
                {
                    if (roleCache.TryGetValue(roleId, out var role) && role != null)
                        return Task.FromResult(role);
                    return client.Rest.GetGuildRoleAsync(Context.Interaction.GuildId.Value, roleId);
                }
            )
        );
        await Task.WhenAll(getRoleTasks);

        var dropdown = new StringMenuProperties(BasicColorsDropdownId)
        {
            MinValues = 0,
            MaxValues = CosmeticRolesSettings.BasicColorRoleIds.Count > 25 ? 25 : CosmeticRolesSettings.BasicColorRoleIds.Count,
            Options = getRoleTasks.Select(task =>
                new StringMenuSelectOptionProperties(task.Result.Name, task.Result.Id.ToString())
                {
                    Default = guildUser.RoleIds.Contains(task.Result.Id),
                }),
        };

        await Context.Interaction.ModifyResponseAsync(msgOpts =>
        {
            msgOpts.Content = "Select color roles:";
            msgOpts.Flags = MessageFlags.Ephemeral;
            msgOpts.Components =
            [
                dropdown,
            ];
        });
    }
}
