using Microsoft.AspNetCore.Mvc;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;
using System.Text.Json;

namespace PROG6212_Part2.Controllers
{
    public class AMController : Controller
    {
        // Path to JSON file storing claims data
        private readonly string _jsonFile =
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "claims.json");

        // Path to directory containing encrypted documents
        private readonly string _encryptedDocsPath =
            Path.Combine(Directory.GetCurrentDirectory(), "Documents");

        private readonly ILogger<AMController> _logger;//Code Attribution (Reppen, 2016)

        public AMController(ILogger<AMController> logger)
        {
            _logger = logger;
        }

        // Display all verified claims
        public IActionResult VerifiedClaims()
        {
            try
            {
                var claims = LoadClaims(); // Load claims from JSON
                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading verified claims.");
                TempData["Error"] = "Unable to load verified claims at this time."; //Code Attribution(farshid jahanmanesh, 2019)
                return View(new List<Teacher>());
            }
        }

        // Approve a specific claim by its ID
        [HttpPost]
        public IActionResult ApproveClaim(Guid claimId)
        {
            try
            {
                UpdateClaimStatus(claimId, "Approved"); // Update claim status
                TempData["Success"] = "Claim approved successfully!";
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Error approving claim {ClaimId}", claimId);
                TempData["Error"] = "Failed to approve claim. Please try again.";
            }
            return RedirectToAction("VerifiedClaims");
        }

        // Reject a specific claim by its ID
        [HttpPost]
        public IActionResult RejectClaim(Guid claimId)
        {
            try
            {
                UpdateClaimStatus(claimId, "Rejected"); // Update claim status
                TempData["Success"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim {ClaimId}", claimId);
                TempData["Error"] = "Failed to reject claim. Please try again.";
            }
            return RedirectToAction("VerifiedClaims");
        }

        // Download and decrypt a document
        public IActionResult DownloadDocument(string fileName)
        {
            try
            {
                var encryptedPath = Path.Combine(_encryptedDocsPath, fileName + ".enc");
                if (!System.IO.File.Exists(encryptedPath))//(dotnet-bot, 2025)
                {
                    TempData["Error"] = "Encrypted document not found.";
                    return RedirectToAction("VerifiedClaims");
                }

                // Create temporary directory for decrypted files
                var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
                Directory.CreateDirectory(tempDir);

                var decryptedPath = Path.Combine(tempDir, fileName);
                AESService.DecryptFile(encryptedPath, decryptedPath); // Decrypt file

                var bytes = System.IO.File.ReadAllBytes(decryptedPath);
                System.IO.File.Delete(decryptedPath); // Securely remove decrypted file

                return File(bytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting or downloading document {FileName}", fileName);
                TempData["Error"] = "Error decrypting or downloading document.";
                return RedirectToAction("VerifiedClaims");
            }
        }

        // Load claims from JSON file
        private List<Teacher> LoadClaims()
        {
            try
            {
                if (!System.IO.File.Exists(_jsonFile))
                    return new List<Teacher>();

                var json = System.IO.File.ReadAllText(_jsonFile);
                var claims = JsonSerializer.Deserialize<List<Teacher>>(json) ?? new List<Teacher>();

                // Ensure each claim has a unique ID
                foreach (var claim in claims.Where(c => c.ClaimId == Guid.Empty))
                    claim.ClaimId = Guid.NewGuid();

                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load claims from JSON file.");
                TempData["Error"] = "Unable to load claims at this time.";
                return new List<Teacher>();
            }
        }

        // Update the status of a claim and persist to JSON
        private void UpdateClaimStatus(Guid claimId, string newStatus)
        {
            try
            {
                var claims = LoadClaims();
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);

                if (claim != null)
                {
                    claim.Status = newStatus; // Set new status
                    var updatedJson = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(_jsonFile, updatedJson); // Save changes
                }
                else
                {
                    _logger.LogWarning("Claim {ClaimId} not found.", claimId);
                    TempData["Warning"] = "Claim not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim status for {ClaimId}", claimId);
                TempData["Error"] = "Failed to update claim status. Please try again.";
            }
        }
    }
}
