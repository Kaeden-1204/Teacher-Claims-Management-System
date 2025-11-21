using System.ComponentModel.DataAnnotations;

namespace PROG6212_Part2.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; } // Primary key for the user

        [Required]
        public string FullName { get; set; } // Full name of the user

        [Required, EmailAddress]
        public string Email { get; set; } // User's email, used for login

        [Required]
        public string Password { get; set; }  // Encrypted password for authentication

        [Required]
        public string Role { get; set; }      // Role of the user: HR, Teacher, PC, AM

        public double? HourlyRate { get; set; }   // Hourly rate, applicable only for Teachers
    }
}
