using Discord;
using Discord.Interactions;

namespace PandaBot.Attributes;

public class DeferAttribute : PreconditionAttribute
{
    private readonly bool _ephemeral;

    public DeferAttribute(bool ephemeral = false)
    {
        _ephemeral = ephemeral;
    }

    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        // Defer the interaction immediately when the precondition is checked
        // This happens BEFORE DI creates the module instance
        if (!context.Interaction.HasResponded)
        {
            await context.Interaction.DeferAsync(_ephemeral);
        }
        
        return PreconditionResult.FromSuccess();
    }
}
