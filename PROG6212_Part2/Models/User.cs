using System.ComponentModel.DataAnnotations;

namespace PROG6212_Part2.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }  // Encrypted using AES

        [Required]
        public string Role { get; set; }      // HR, Lecturer, PC, AM

        public double? HourlyRate { get; set; }   // Only lecturers need it
    }
}