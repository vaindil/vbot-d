using Microsoft.EntityFrameworkCore;
using VainBotDiscord.Classes;

namespace VainBotDiscord
{
    public class VbContext : DbContext
    {
        public VbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<KeyValue> KeyValues { get; set; }

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
        }
    }
}
