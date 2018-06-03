using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace VainBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class FitzyAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command,
            IServiceProvider services)
        {
            var appInfo = await context.Client.GetApplicationInfoAsync();

            var user = context.Message.Author as SocketGuildUser;
            return user.Id == appInfo.Owner.Id || user.GuildPermissions.Administrator
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You do not have permission to use that command.");
        }
    }
}
