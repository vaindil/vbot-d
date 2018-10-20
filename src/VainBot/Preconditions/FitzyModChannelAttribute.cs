using Discord.Commands;
using System;
using System.Linq;
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
            var validChannelIds = new ulong[]
            {
                313643739719532544,
                432328775598866434,
                480178651837628436,
                503214247195574302
            };

            return validChannelIds.Contains(context.Channel.Id)
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("That command cannot be used in this channel."));
        }
    }
}
