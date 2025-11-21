using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly AESService _aesService; // AES encryption service instance

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            _aesService = new AESService(); // Initialize AES service
        }

        public DbSet<User> Users { get; set; } // Users table
        public DbSet<Claim> Claims { get; set; } // Claims table
        public DbSet<ClaimDocument> ClaimDocuments { get; set; } // Claim documents table

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call base configuration

            // Seed default system users for login
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "HR Admin",
                    Email = "hr@university.ac.za",
                    Password = "Hr!2025$Secure", // HR user password
                    Role = "HR" // HR role
                },
                new User
                {
                    UserId = 2,
                    FullName = "Programme Coordinator",
                    Email = "pc@university.ac.za",
                    Password = "Pc@2025#Coord", // PC user password
                    Role = "PC" // PC role
                },
                new User
                {
                    UserId = 3,
                    FullName = "Academic Manager",
                    Email = "am@university.ac.za",
                    Password = "Am2025*Manager", // AM user password
                    Role = "AM" // AM role
                }
            );
        }

    }
}
