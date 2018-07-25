using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace VainBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CrendorModAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var role = context.Guild.GetRole(149054540673581056);
            if (context.Guild.Id == 149051954348294145
                && ((SocketGuildUser)context.Message.Author).Hierarchy >= role.Position)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError("You do not have permission to use that command."));
        }
    }
}
