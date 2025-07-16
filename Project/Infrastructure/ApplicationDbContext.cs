using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Player
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Game
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasMany(e => e.Moves)
                      .WithOne()
                      .HasForeignKey(e => e.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Move
            modelBuilder.Entity<Move>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Уникальность клетки в пределах игры (один ход на клетку)
                entity.HasIndex(e => new { e.GameId, e.X, e.Y }).IsUnique();

                // Уникальность ClientMoveId для идемпотентности
                entity.HasIndex(e => new { e.GameId, e.ClientMoveId }).IsUnique();

                // Ограничения на длину строк, если надо
                entity.Property(e => e.ClientMoveId)
                      .IsRequired()
                      .HasMaxLength(100); // или другое значение, в зависимости от формата
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}