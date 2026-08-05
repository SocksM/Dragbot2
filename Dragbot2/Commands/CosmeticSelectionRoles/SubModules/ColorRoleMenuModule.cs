using System.Text;
using Dragbot2.Resources.AppSettings;
using Dragbot2.Services;
using Dragbot2.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Dragbot2.Commands.CosmeticSelectionRoles.SubModules;

public class ColorRoleMenuModule(
    ILogger<ColorRoleMenuModule> logger,
    IOptions<CosmeticRolesSettings> optCosmeticRolesSettings,
    GuildUserService guildUserService
) : ComponentInteractionModule<StringMenuInteractionContext>
{
    private CosmeticRolesSettings CosmeticRolesSettings => optCosmeticRolesSettings.Value;

    [ComponentInteraction(CosmeticSelectionRolesCommands.BasicColorsDropdownId)]
    public async Task HandleBasicColorRolesSelected()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await HandleRolesSelected("basic", GetAllowedRoleIds);
        return;

        List<ulong> GetAllowedRoleIds(GuildUser guildUser) => CosmeticSelectionRolesCommands.GetAllowedBasicColorRoleIds(guildUser, CosmeticRolesSettings);
    }

    [ComponentInteraction(CosmeticSelectionRolesCommands.LevelColorsDropdownId)]
    public async Task HandleLevelColorsSelected()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        await HandleRolesSelected("level", GetAllowedRoleIds);
        return;

        List<ulong> GetAllowedRoleIds(GuildUser guildUser) => CosmeticSelectionRolesCommands.GetAllowedRoleRequirementColorRoles(guildUser, CosmeticRolesSettings).Select(pair => pair.Key).ToList();
    }

    private enum HasDisallowedRolesSelectedResult
    {
        True,
        False,
        ParsingFailed,
    }

    private async Task<HasDisallowedRolesSelectedResult> HasDisallowedRolesSelected(List<ulong> allowedRoleIds)
    {
        foreach (var selectedValue in Context.SelectedValues)
        {
            bool parseSuccess = ulong.TryParse(selectedValue, out ulong id);
            if (!parseSuccess)
            {
                logger.LogError("A ulong.TryParse failed for the selected value: {selectedValue}", selectedValue);
                await Context.Interaction.ModifyResponseAsync(msgOpts =>
                {
                    msgOpts.Content = "Something went wrong when trying to figure out which roles you had selected";
                    msgOpts.Flags = MessageFlags.Ephemeral;
                });
                return HasDisallowedRolesSelectedResult.ParsingFailed;
            }

            if (!allowedRoleIds.Contains(id))
            {
                logger.LogWarning("A dissallowed role was passed to a color role menu selection module, roleId: {roleId}, by userId {userId}",
                    selectedValue, Context.User.Id);
                await Context.Interaction.ModifyResponseAsync(msgOpts => { });
                return HasDisallowedRolesSelectedResult.True;
            }
        }

        return HasDisallowedRolesSelectedResult.False;
    }

    private async Task HandleRolesSelected(string? roleType, Func<GuildUser, List<ulong>> getAllowedRoleIds)
    {
        if (!Context.Interaction.GuildId.HasValue)
        {
            await InteractionContextUtils.CouldntFindGuildInContext(logger, Context);
            return;
        }

        GuildUser guildUser = await guildUserService.GetGuildUser(Context.Interaction.GuildId.Value, Context.User.Id);
        List<ulong> allowedRoleIds = getAllowedRoleIds(guildUser);

        var hasDisallowedRolesSelectedResult = await HasDisallowedRolesSelected(allowedRoleIds);
        if (hasDisallowedRolesSelectedResult is HasDisallowedRolesSelectedResult.ParsingFailed or HasDisallowedRolesSelectedResult.True)
            return;

        List<ulong> selectedRoleIds = Context.SelectedValues.Select(ulong.Parse).ToList();
        await guildUserService.RemoveRolesFromGuildUser(Context.Interaction.GuildId.Value, Context.User.Id, allowedRoleIds.Where(roleId => !selectedRoleIds.Contains(roleId)).ToList());
        await guildUserService.AddRolesToGuildUser(Context.Interaction.GuildId.Value, Context.User.Id, selectedRoleIds);

        string msgStr;
        if (Context.SelectedValues.Count == 0)
        {
            if (roleType == null)
                msgStr = "You now have no color roles of this type.";
            else
                msgStr = $"You now have no {roleType} color roles.";
        }
        else
        {
            StringBuilder sb = new("You now have the following ");
            if (roleType != null)
                sb.Append(roleType).Append(' ');
            sb.Append("color role");
            if (Context.SelectedValues.Count > 1) sb.Append('s');
            sb.Append(":\n");
            foreach (var roleId in selectedRoleIds)
            {
                sb.Append("- <@&").Append(roleId).Append(">\n");
            }

            msgStr = sb.ToString();
        }

        await Context.Interaction.ModifyResponseAsync(msgOpts =>
        {
            msgOpts.Content = msgStr;
            msgOpts.Flags = MessageFlags.Ephemeral;
            msgOpts.AllowedMentions = new AllowedMentionsProperties
            {
                AllowedRoles = [],
            };
        });
    }
}
