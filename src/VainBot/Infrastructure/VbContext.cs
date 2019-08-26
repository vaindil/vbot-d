using Microsoft.EntityFrameworkCore;
using VainBot.Classes.Reminders;

namespace VainBot.Infrastructure
{
    public class VbContext : DbContext
    {
        public VbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
            });
        }
    }
}
