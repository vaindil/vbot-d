using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VainBot.Classes.Users;
using VainBot.Infrastructure;

namespace VainBot.Modules
{
    public class UserModule : ModuleBase
    {
        private readonly VbContext _db;

        public UserModule(VbContext db)
        {
            _db = db;
        }

        //[Command]
        //[Alias("help")]
        public async Task Help()
        {
            await ReplyAsync("This will have help text, but I have to write the commands in the first place. :thinking:");
        }

        [Command("track")]
        public async Task CreateDiscordUser(IUser user)
        {

        }
    }
}
