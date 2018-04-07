using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace VainBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class FitzyGuildAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            if (context.Guild.Id == 294643578301448194)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(PreconditionResult.FromError("Command cannot be used in this server."));
        }
    }
}
