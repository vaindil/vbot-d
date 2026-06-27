using System;

namespace VainBot.Classes.FriendTime
{
    public class FriendTime
    {
        public int Id { get; set; }

        public long GuildId { get; set; }

        public long ChannelId { get; set; }

        public long UserId { get; set; }

        public string Timezone { get; set; }
    }
}
