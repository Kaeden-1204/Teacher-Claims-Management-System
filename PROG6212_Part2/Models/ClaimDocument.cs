using System.ComponentModel.DataAnnotations;

namespace PROG6212_Part2.Models
{
    public class ClaimDocument
    {
        [Key]
        public int DocumentId { get; set; } // Primary key for the document

        [Required]
        public string FileName { get; set; } // Original file name uploaded by the user

        [Required]
        public string FilePath { get; set; } // Path to the stored (encrypted) file on the server

        public int ClaimId { get; set; } // Foreign key linking this document to its parent Claim
        public Claim Claim { get; set; } // Navigation property for the associated Claim
    }
}
