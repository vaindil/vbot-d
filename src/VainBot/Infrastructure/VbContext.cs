using Microsoft.EntityFrameworkCore;
using VainBot.Classes;
using VainBot.Classes.Reminders;
using VainBot.Classes.Smugboard;
using VainBot.Classes.Twitch;
using VainBot.Classes.Twitter;
using VainBot.Classes.YouTube;

namespace VainBot.Infrastructure
{
    public class VbContext : DbContext
    {
        public VbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<TwitchStreamToCheck> StreamsToCheck { get; set; }
        public DbSet<TwitchLiveStream> TwitchLiveStreams { get; set; }
        public DbSet<YouTubeChannelToCheck> YouTubeChannelsToCheck { get; set; }
        public DbSet<TwitterToCheck> TwittersToCheck { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<SmugboardMessage> SmugboardMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TwitchStreamToCheck>(e =>
            {
                e.ToTable("twitch_stream_to_check");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.TwitchId).HasColumnName("twitch_id").IsRequired();
                e.Property(t => t.Username).HasColumnName("username").IsRequired();
                e.Property(t => t.MessageToPost).HasColumnName("message_to_post");
                e.Property(t => t.ChannelId).HasColumnName("channel_id").IsRequired();
                e.Property(t => t.GuildId).HasColumnName("guild_id").IsRequired();
                e.Property(t => t.IsEmbedded).HasColumnName("is_embedded").IsRequired();
                e.Property(t => t.IsDeleted).HasColumnName("is_deleted").IsRequired();
                e.Property(t => t.CurrentMessageId).HasColumnName("current_message_id");
            });

            modelBuilder.Entity<TwitchLiveStream>(e =>
            {
                e.ToTable("twitch_live_stream");
                e.HasKey(t => t.TwitchUserId);

                e.Property(t => t.TwitchUserId).HasColumnName("twitch_user_id").IsRequired();
                e.Property(t => t.StartedAt).HasColumnName("started_at").IsRequired();
                e.Property(t => t.FirstOfflineAt).HasColumnName("first_offline_at");
                e.Property(t => t.TwitchStreamId).HasColumnName("twitch_stream_id").IsRequired();
                e.Property(t => t.TwitchLogin).HasColumnName("twitch_login").IsRequired();
                e.Property(t => t.TwitchDisplayName).HasColumnName("twitch_display_name").IsRequired();
                e.Property(t => t.ViewerCount).HasColumnName("viewer_count").IsRequired();
                e.Property(t => t.Title).HasColumnName("title").IsRequired();
                e.Property(t => t.GameName).HasColumnName("game_name").IsRequired();
                e.Property(t => t.GameId).HasColumnName("game_id").IsRequired();
                e.Property(t => t.ThumbnailUrl).HasColumnName("thumbnail_url").IsRequired();
                e.Property(t => t.ProfileImageUrl).HasColumnName("profile_image_url").IsRequired();
            });

            modelBuilder.Entity<YouTubeChannelToCheck>(e =>
            {
                e.ToTable("youtube_channel_to_check");
                e.HasKey(y => y.Id);

                e.Property(y => y.Id).HasColumnName("id");
                e.Property(y => y.Username).HasColumnName("username").IsRequired();
                e.Property(y => y.DiscordGuildId).HasColumnName("discord_guild_id").IsRequired();
                e.Property(y => y.DiscordChannelId).HasColumnName("discord_channel_id").IsRequired();
                e.Property(y => y.DiscordMessageToPost).HasColumnName("discord_message_to_post");
                e.Property(y => y.YouTubeChannelId).HasColumnName("youtube_channel_id").IsRequired();
                e.Property(y => y.YouTubePlaylistId).HasColumnName("youtube_playlist_id").IsRequired();
                e.Property(y => y.IsDeleted).HasColumnName("is_deleted").IsRequired();
                e.Property(y => y.LatestVideoId).HasColumnName("latest_video_id");
                e.Property(y => y.LatestVideoUploadedAt).HasColumnName("latest_video_uploaded_at");
                e.Property(y => y.DiscordMessageId).HasColumnName("discord_message_id");
            });

            modelBuilder.Entity<TwitterToCheck>(e =>
            {
                e.ToTable("twitter_to_check");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.TwitterUsername).HasColumnName("twitter_username").IsRequired();
                e.Property(t => t.TwitterId).HasColumnName("twitter_id").IsRequired();
                e.Property(t => t.IncludeRetweets).HasColumnName("include_retweets").IsRequired();
                e.Property(t => t.DiscordGuildId).HasColumnName("discord_guild_id").IsRequired();
                e.Property(t => t.DiscordChannelId).HasColumnName("discord_channel_id").IsRequired();
                e.Property(t => t.LatestTweetId).HasColumnName("latest_tweet_id").IsRequired();
            });

            modelBuilder.Entity<Reminder>(e =>
            {
                e.ToTable("reminder");
                e.HasKey(r => r.Id);

                e.Property(r => r.Id).HasColumnName("id");
                e.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
                e.Property(r => r.FireAt).HasColumnName("fire_at").IsRequired();
                e.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
                e.Property(r => r.ChannelId).HasColumnName("channel_id").IsRequired();
                e.Property(r => r.RequestingMessageId).HasColumnName("requesting_message_id").IsRequired();
                e.Property(r => r.GuildId).HasColumnName("guild_id");
                e.Property(r => r.Message).HasColumnName("message").IsRequired();
                e.Property(r => r.IsActive).HasColumnName("is_active").IsRequired();
            });

            modelBuilder.Entity<SmugboardMessage>(e =>
            {
                e.ToTable("smugboard_message");
                e.HasKey(r => r.MessageId);

                e.Property(s => s.MessageId).HasColumnName("message_id");
            });
        }
    }
}
