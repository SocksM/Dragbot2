using System.Text;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Dragbot2.Commands.CosmeticSelectionRoles.SubModules;

public class BasicRoleMenuModule(GatewayClient client, ILogger<BasicRoleMenuModule> logger) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction(CosmeticSelectionRolesCommands.BasicColorsDropdownId)]
    public async Task HandleRolesSelected()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        List<ulong> selectedRoleIds = Context.SelectedValues.Select(ulong.Parse).ToList();

        if (!Context.Interaction.GuildId.HasValue)
        {
            logger.LogError("Couldn't find guild in the context");
            await Context.Interaction.ModifyResponseAsync(msgOpts =>
            {
                msgOpts.Content = "Something went wrong, please try again later.";
                msgOpts.Flags = MessageFlags.Ephemeral;
            });
            return;
        }

        List<Task> addRoleTasks = [];
        addRoleTasks.AddRange(
            from roleId in selectedRoleIds
            select client.Rest.AddGuildUserRoleAsync(Context.Interaction.GuildId.Value, Context.User.Id, roleId)
        );
        await Task.WhenAll(addRoleTasks);
        logger.LogInformation("Gave user {userId}, {roleCount} roles", Context.User.Id, addRoleTasks.Count);

        string msgStr;
        if (Context.SelectedValues.Count == 0)
        {
            msgStr = "You now have no basic color roles.";
        }
        else
        {
            StringBuilder sb = new("You now have the following basic color role");
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
