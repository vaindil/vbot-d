using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace VainBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class FitzyModChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            return context.Channel.Id == 313643739719532544 || context.Channel.Id == 432328775598866434 || context.Channel.Id == 480178651837628436
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("That command cannot be used in this channel."));
        }
    }
}
