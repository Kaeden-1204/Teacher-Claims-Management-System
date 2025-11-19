using System.ComponentModel.DataAnnotations;

namespace PROG6212_Part2.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }  

        [Required]
        public string Role { get; set; }      

        public double? HourlyRate { get; set; }   
    }
}