using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services; 

namespace PROG6212_Part2.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly AESService _aesService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            _aesService = new AESService();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

       
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "HR Admin",
                    Email = "hr@university.ac.za",
                    Password = "Hr!2025$Secure", 
                    Role = "HR"
                },
                new User
                {
                    UserId = 2,
                    FullName = "Programme Coordinator",
                    Email = "pc@university.ac.za",
                    Password = "Pc@2025#Coord", 
                    Role = "PC"
                },
                new User
                {
                    UserId = 3,
                    FullName = "Academic Manager",
                    Email = "am@university.ac.za",
                    Password = "Am2025*Manager", 
                    Role = "AM"
                }
            );
        }

    }
}

