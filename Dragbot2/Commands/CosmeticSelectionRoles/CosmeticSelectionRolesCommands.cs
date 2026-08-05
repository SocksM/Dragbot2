using Dragbot2.Resources.AppSettings;
using Dragbot2.Services;
using Dragbot2.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Dragbot2.Commands.CosmeticSelectionRoles;

[SlashCommand("cosmetic-roles", "Select cosmetic roles")]
public class CosmeticSelectionRolesCommands(
    IOptions<CosmeticRolesSettings> optCosmeticRolesSettings,
    GatewayClient client,
    ILogger<CosmeticSelectionRolesCommands> logger,
    RoleService roleService,
    GuildUserService guildUserService
) : ApplicationCommandModule<ApplicationCommandContext>
{
    private CosmeticRolesSettings CosmeticRolesSettings => optCosmeticRolesSettings.Value;

    public const string BasicColorsDropdownId = "basic-colors-dropdown";
    public const string LevelColorsDropdownId = "level-colors-dropdown";

    [SubSlashCommand("basic-colors", "Select basic colors")]
    public async Task BasicColorSelection()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        if (!Context.Interaction.GuildId.HasValue)
        {
            await InteractionContextUtils.CouldntFindGuildInContext(logger, Context);
            return;
        }

        GuildUser guildUser = await guildUserService.GetGuildUser(Context.Interaction.GuildId.Value, Context.User.Id);
        List<ulong> allowedRoleIds = GetAllowedBasicColorRoleIds(guildUser);

        if (allowedRoleIds.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(msgOpts =>
            {
                msgOpts.Content = $"You do not have the required role to use this command.\nRole required: <@&{CosmeticRolesSettings.RequiredRoleIdForBasicColors!.Value}>";
                msgOpts.Flags = MessageFlags.Ephemeral;
                msgOpts.AllowedMentions = new AllowedMentionsProperties
                {
                    AllowedRoles = [],
                };
            });
            return;
        }

        Role[] basicColorRoles = await roleService.GetGuildRoles(Context.Interaction.GuildId.Value, CosmeticRolesSettings.BasicColorRoleIds);

        var dropdown = new StringMenuProperties(BasicColorsDropdownId)
        {
            MinValues = 0,
            MaxValues = CosmeticRolesSettings.BasicColorRoleIds.Count > 25 ? 25 : CosmeticRolesSettings.BasicColorRoleIds.Count,
            Options = basicColorRoles.Select(role =>
                new StringMenuSelectOptionProperties(role.Name, role.Id.ToString())
                {
                    Default = guildUser.RoleIds.Contains(role.Id),
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

    [SubSlashCommand("level-colors", "Select level colors")]
    public async Task LevelColorSelection()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        if (!Context.Interaction.GuildId.HasValue)
        {
            await InteractionContextUtils.CouldntFindGuildInContext(logger, Context);
            return;
        }

        GuildUser guildUser = await client.Rest.GetGuildUserAsync(Context.Interaction.GuildId.Value, Context.User.Id);
        Dictionary<ulong, ulong> acquiredUserRequirementRoles = GetAllowedRoleRequirementColorRoles(guildUser);
        if (acquiredUserRequirementRoles.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(msgOpts =>
            {
                msgOpts.Content = "You don't meet the requirements for any of the unlockable level colors";
                msgOpts.Flags = MessageFlags.Ephemeral;
            });
            return;
        }

        Role[] unlockedRoles = await roleService.GetGuildRoles(Context.Interaction.GuildId.Value, acquiredUserRequirementRoles.Select(reqPair => reqPair.Key));

        var dropdown = new StringMenuProperties(LevelColorsDropdownId)
        {
            MinValues = 0,
            MaxValues = unlockedRoles.Length > 25 ? 25 : unlockedRoles.Length,
            Options = unlockedRoles.Select(role =>
                new StringMenuSelectOptionProperties(role.Name, role.Id.ToString())
                {
                    Default = guildUser.RoleIds.Contains(role.Id),
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

    public List<ulong> GetAllowedBasicColorRoleIds(GuildUser guildUser) =>
        GetAllowedBasicColorRoleIds(guildUser, CosmeticRolesSettings);

    public static List<ulong> GetAllowedBasicColorRoleIds(GuildUser guildUser, CosmeticRolesSettings crs)
    {
        if (!crs.RequiredRoleIdForBasicColors.HasValue) return crs.BasicColorRoleIds;
        return guildUser.RoleIds.Contains(crs.RequiredRoleIdForBasicColors.Value) ? crs.BasicColorRoleIds : [];
    }

    public Dictionary<ulong, ulong> GetAllowedRoleRequirementColorRoles(GuildUser guildUser) =>
        GetAllowedRoleRequirementColorRoles(guildUser, CosmeticRolesSettings);

    public static Dictionary<ulong, ulong> GetAllowedRoleRequirementColorRoles(GuildUser guildUser, CosmeticRolesSettings crs)
    {
        return crs.RoleRequirementColorRoles.Where(reqPair =>
            guildUser.RoleIds.Contains(reqPair.Value)
        ).ToDictionary();
    }
}
