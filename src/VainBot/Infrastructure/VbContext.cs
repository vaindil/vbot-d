using Microsoft.EntityFrameworkCore;
using System;
using VainBot.Classes;
using VainBot.Classes.Reminders;
using VainBot.Classes.Twitch;
using VainBot.Classes.Twitter;
using VainBot.Classes.Users;
using VainBot.Classes.YouTube;

namespace VainBot.Infrastructure
{
    public class VbContext : DbContext
    {
        public VbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<KeyValue> KeyValues { get; set; }
        public DbSet<TwitchStreamToCheck> StreamsToCheck { get; set; }
        public DbSet<TwitchLiveStream> TwitchLiveStreams { get; set; }
        public DbSet<YouTubeChannelToCheck> YouTubeChannelsToCheck { get; set; }
        public DbSet<TwitterToCheck> TwittersToCheck { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAlias> UserAliases { get; set; }
        public DbSet<DiscordUsernameHistory> DiscordUsernameHistories { get; set; }
        public DbSet<TwitchUsernameHistory> TwitchUsernameHistories { get; set; }
        public DbSet<ActionTaken> ActionsTaken { get; set; }
        public DbSet<UserNote> UserNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<KeyValue>(e =>
            {
                e.ToTable("key_value");
                e.HasKey(kv => kv.Key);

                e.Property(kv => kv.Key).HasColumnName("key").IsRequired().HasMaxLength(100);
                e.Property(kv => kv.Value).HasColumnName("value").IsRequired().HasMaxLength(250);
            });

            modelBuilder.Entity<TwitchStreamToCheck>(e =>
            {
                e.ToTable("twitch_stream_to_check");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.TwitchId).HasColumnName("twitch_id").IsRequired().HasMaxLength(50);
                e.Property(t => t.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
                e.Property(t => t.MessageToPost).HasColumnName("message_to_post").IsRequired().HasMaxLength(1500);
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

                e.Property(t => t.TwitchUserId).HasColumnName("twitch_user_id").IsRequired().HasMaxLength(50);
                e.Property(t => t.StartedAt).HasColumnName("started_at").IsRequired();
                e.Property(t => t.FirstOfflineAt).HasColumnName("first_offline_at");
                e.Property(t => t.TwitchStreamId).HasColumnName("twitch_stream_id").IsRequired().HasMaxLength(50);
                e.Property(t => t.TwitchLogin).HasColumnName("twitch_login").IsRequired().HasMaxLength(50);
                e.Property(t => t.TwitchDisplayName).HasColumnName("twitch_display_name").IsRequired().HasMaxLength(50);
                e.Property(t => t.ViewerCount).HasColumnName("viewer_count").IsRequired();
                e.Property(t => t.Title).HasColumnName("title").IsRequired().HasMaxLength(200);
                e.Property(t => t.GameName).HasColumnName("game_name").IsRequired().HasMaxLength(300);
                e.Property(t => t.GameId).HasColumnName("game_id").IsRequired().HasMaxLength(50);
                e.Property(t => t.ThumbnailUrl).HasColumnName("thumbnail_url").IsRequired().HasMaxLength(350);
                e.Property(t => t.ProfileImageUrl).HasColumnName("profile_image_url").IsRequired().HasMaxLength(350);
            });

            modelBuilder.Entity<YouTubeChannelToCheck>(e =>
            {
                e.ToTable("youtube_channel_to_check");
                e.HasKey(y => y.Id);

                e.Property(y => y.Id).HasColumnName("id");
                e.Property(y => y.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
                e.Property(y => y.DiscordGuildId).HasColumnName("discord_guild_id").IsRequired();
                e.Property(y => y.DiscordChannelId).HasColumnName("discord_channel_id").IsRequired();
                e.Property(y => y.DiscordMessageToPost).HasColumnName("discord_message_to_post").IsRequired().HasMaxLength(200);
                e.Property(y => y.YouTubeChannelId).HasColumnName("youtube_channel_id").IsRequired().HasMaxLength(40);
                e.Property(y => y.YouTubePlaylistId).HasColumnName("youtube_playlist_id").IsRequired().HasMaxLength(40);
                e.Property(y => y.IsDeleted).HasColumnName("is_deleted").IsRequired();
                e.Property(y => y.LatestVideoId).HasColumnName("latest_video_id").HasMaxLength(40);
                e.Property(y => y.LatestVideoUploadedAt).HasColumnName("latest_video_uploaded_at");
                e.Property(y => y.DiscordMessageId).HasColumnName("discord_message_id");
            });

            modelBuilder.Entity<TwitterToCheck>(e =>
            {
                e.ToTable("twitter_to_check");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.TwitterUsername).HasColumnName("twitter_username").IsRequired().HasMaxLength(100);
                e.Property(t => t.TwitterId).HasColumnName("twitter_id").IsRequired();
                e.Property(t => t.IncludeRetweets).HasColumnName("include_retweets").IsRequired();
                e.Property(t => t.DiscordGuildId).HasColumnName("discord_guild_id").IsRequired();
                e.Property(t => t.DiscordChannelId).HasColumnName("discord_channel_id").IsRequired();
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
                e.Property(r => r.GuildId).HasColumnName("guild_id").IsRequired();
                e.Property(r => r.IsDM).HasColumnName("is_dm").IsRequired();
                e.Property(r => r.Message).HasColumnName("message").IsRequired().HasMaxLength(500);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("tracked_user");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.TwitchId).HasColumnName("twitch_id");
                e.Property(t => t.DiscordId).HasColumnName("discord_id");
                e.Property(t => t.IsModerator).HasColumnName("is_moderator").IsRequired();
            });

            modelBuilder.Entity<UserAlias>(e =>
            {
                e.ToTable("user_alias");
                e.HasKey(u => u.Id);

                e.Property(u => u.Id).HasColumnName("id");
                e.Property(u => u.UserId).HasColumnName("user_id").IsRequired();
                e.Property(u => u.ModeratorId).HasColumnName("moderator_id").IsRequired();
                e.Property(u => u.AddedAt).HasColumnName("added_at").IsRequired();
                e.Property(u => u.Alias).HasColumnName("alias").IsRequired();

                e.HasOne(u => u.User)
                    .WithMany(x => x.Aliases)
                    .HasForeignKey(u => u.UserId);

                e.HasOne(u => u.Moderator)
                    .WithMany(m => m.ModeratedAliases)
                    .HasForeignKey(u => u.ModeratorId);
            });

            modelBuilder.Entity<TwitchUsernameHistory>(e =>
            {
                e.ToTable("twitch_username_history");
                e.HasKey(t => t.Id);

                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
                e.Property(t => t.LoggedAt).HasColumnName("logged_at").IsRequired();
                e.Property(t => t.Username).HasColumnName("username").IsRequired();

                e.HasOne(t => t.User)
                    .WithMany(u => u.TwitchUsernames)
                    .HasForeignKey(t => t.UserId);
            });

            modelBuilder.Entity<DiscordUsernameHistory>(e =>
            {
                e.ToTable("discord_username_history");
                e.HasKey(d => d.Id);

                e.Property(d => d.Id).HasColumnName("id");
                e.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
                e.Property(d => d.LoggedAt).HasColumnName("logged_at").IsRequired();
                e.Property(d => d.Username).HasColumnName("username").IsRequired();
                e.Property(d => d.Discriminator).HasColumnName("discriminator").IsRequired();

                e.HasOne(d => d.User)
                    .WithMany(u => u.DiscordUsernames)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<ActionTaken>(e =>
            {
                e.ToTable("action_taken");
                e.HasKey(a => a.Id);

                e.Property(a => a.Id).HasColumnName("id");
                e.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
                e.Property(a => a.ModeratorId).HasColumnName("moderator_id").IsRequired();
                e.Property(a => a.LoggedAt).HasColumnName("logged_at").IsRequired();
                e.Property(a => a.DurationSeconds).HasColumnName("duration_seconds").IsRequired();
                e.Property(a => a.Reason).HasColumnName("reason");
                e.Property(a => a.ActionTakenType).HasColumnName("action_type").IsRequired()
                    .HasConversion((x) => x.ToString(), (x) => (ActionTakenType)Enum.Parse(typeof(ActionTakenType), x));

                e.HasOne(a => a.User)
                    .WithMany(u => u.ActionsAgainst)
                    .HasForeignKey(a => a.UserId);

                e.HasOne(a => a.Moderator)
                    .WithMany(m => m.ModeratedActions)
                    .HasForeignKey(a => a.ModeratorId);
            });

            modelBuilder.Entity<UserNote>(e =>
            {
                e.ToTable("user_note");
                e.HasKey(n => n.Id);

                e.Property(n => n.Id).HasColumnName("id");
                e.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
                e.Property(n => n.ModeratorId).HasColumnName("moderator_id").IsRequired();
                e.Property(n => n.LoggedAt).HasColumnName("logged_at").IsRequired();
                e.Property(n => n.Note).HasColumnName("note").IsRequired();

                e.HasOne(n => n.User)
                    .WithMany(u => u.Notes)
                    .HasForeignKey(n => n.UserId);

                e.HasOne(n => n.Moderator)
                    .WithMany(m => m.ModeratedNotes)
                    .HasForeignKey(n => n.ModeratorId);
            });
        }
    }
}
