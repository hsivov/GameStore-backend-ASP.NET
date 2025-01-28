using GameStore.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Game>(entity =>
            {
                entity.Property(g => g.Price).HasPrecision(18, 2);
            });

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OwnedGames)  // ApplicationUser can own many Games
                .WithMany()                  // Game can have many ApplicationUsers
                .UsingEntity(j => j.ToTable("users_games")); // Join table for the many-to-many relationship

            modelBuilder.Entity<ShoppingCart>()
                .HasMany(sc => sc.Games)
                .WithMany()
                .UsingEntity(j => j.ToTable("shopping_carts_games"));

            modelBuilder.Entity<Order>()
                .HasMany(o => o.BoughtGames)
                .WithMany(g => g.Orders)
                .UsingEntity(j => j.ToTable("orders_games"));

            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Action", Description= "The player overcomes challenges by physical means such as precise aim and quick response times." },
                new Genre { Id = 2, Name = "Adventure", Description= "The player assumes the role of a protagonist in an interactive story, driven by exploration and/or puzzle-solving." },
                new Genre { Id = 3, Name = "RPG", Description= "Role-playing games (or RPGs) are video games where players engage with the gameworld through characters who have backstories and existing motivations. " +
                "The RPG genre often includes NPCs (non-player characters), side quests, downloadable content (dlc), and larger story arcs." },
                new Genre { Id = 4, Name = "Simulation", Description= "Games that are designed to mimic activities you'd see in the real world. The purpose of the game may be to teach you something. For example, you could learn how to fish. " +
                "Others simulation games take on operating a business such as a farm or a theme park." },
                new Genre { Id = 5, Name = "Strategy", Description= "Players succeed (or lose) based on strategic decisions, not luck. Players have equal knowledge to play; no trivia. " +
                "Play is based on multiple decisions a person could make on each turn with possible advantages and disadvantages each time." },
                new Genre { Id = 6, Name = "Sports", Description = "Games that simulate playing real-world sports. Most sports have been recreated with a game, including team sports, track and field, extreme sports, and combat sports." },
                new Genre { Id = 7, Name = "MMO", Description = "Massively multiplayer online games (MMOs) are games that are played online by hundreds or thousands of players. " +
                "They are different from other multiplayer games, as they have no overall winner and players can join and leave as they wish."}
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
        }
        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Order> Orders { get; set; }
    }
}
