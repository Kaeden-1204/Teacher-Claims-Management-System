using System.ComponentModel.DataAnnotations;

namespace PROG6212_Part2.Models
{
    public class ClaimDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public string FileName { get; set; }      

        [Required]
        public string FilePath { get; set; }      

        
        public int ClaimId { get; set; }
        public Claim Claim { get; set; }
    }
}