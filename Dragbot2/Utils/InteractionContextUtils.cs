using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Services;

namespace Dragbot2.Utils;

public static class InteractionContextUtils
{
    public static async Task CouldntFindGuildInContext(ILogger logger, IInteractionContext interactionContext)
    {
        logger.LogError("Couldn't find guild in the context");
        await interactionContext.Interaction.ModifyResponseAsync(msgOpts =>
        {
            msgOpts.Content = "Something went wrong, please try again later.";
            msgOpts.Flags = MessageFlags.Ephemeral;
        });
    }
}
