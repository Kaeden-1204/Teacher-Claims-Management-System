using System.ComponentModel.DataAnnotations;  
using System.Text.Json.Serialization;          
using Microsoft.AspNetCore.Http;               

namespace PROG6212_Part2.Models
{
    public class Teacher
    {
        // Unique identifier for each claim (automatically generated)
        [Key]
        public Guid ClaimId { get; set; } = Guid.NewGuid();//(dotnet-bot, 2025a)

        // Full name of the teacher submitting the claim (required field)
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        // Subject or department the teacher belongs to
        [Required]
        public string Subject { get; set; }

        // Teacher's contact email with email format validation
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Date the claim was made
        [Required]
        [DataType(DataType.Date)]
        public DateTime ClaimDate { get; set; }

        // Number of hours worked being claimed for
        [Required]
        [Range(0, double.MaxValue)]
        public double HoursWorked { get; set; }

        // Hourly pay rate for the teacher
        [Required]
        [Range(0, double.MaxValue)]
        public double HourlyRate { get; set; }

        // Computed property that automatically calculates the total claim amount
        [Display(Name = "Total Amount (R)")]
        public double TotalAmount => HoursWorked * HourlyRate;

        // Optional notes field for additional information about the claim
        public string? Notes { get; set; }

        // List of uploaded supporting documents (excluded from JSON to prevent serialization errors)
        [Display(Name = "Supporting Documents")]
        [JsonIgnore]
        public List<IFormFile>? SupportingDocuments { get; set; }

        // Stores the names of files saved after encryption for retrieval or tracking
        public List<string>? SavedFiles { get; set; } = new List<string>();

        // Current status of the claim (default is "Pending")
        public string Status { get; set; } = "Pending";
    }
}
