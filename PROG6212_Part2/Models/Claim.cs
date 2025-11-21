using System.ComponentModel.DataAnnotations;  
using System.Text.Json.Serialization;          
using Microsoft.AspNetCore.Http;               

namespace PROG6212_Part2.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

       
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ClaimDate { get; set; }

        [Required]
        public double HoursWorked { get; set; }

        [Required]
        public double HourlyRate { get; set; }

        public double TotalAmount { get; set; }

        public string? Notes { get; set; }

        
        public string Status { get; set; } = "Pending";

        
        public List<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();

        public int UserId { get; set; }            // foreign key to User
        public User? User { get; set; }            // navigation property

    }
}

