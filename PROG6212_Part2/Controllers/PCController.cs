using Microsoft.AspNetCore.Mvc;     
using PROG6212_Part2.Models;       
using PROG6212_Part2.Services;     
using System.Text.Json;            

namespace PROG6212_Part2.Controllers
{
    public class PCController : Controller
    {
        // Path to JSON file storing teacher claim data
        private readonly string _jsonFile =
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "claims.json");

        // Directory path where encrypted claim documents are stored
        private readonly string _encryptedDocsPath =
            Path.Combine(Directory.GetCurrentDirectory(), "Documents");

        // ASP.NET Core built-in logger for structured logging and error tracking
        private readonly ILogger<PCController> _logger;

        // Constructor with dependency injection for ILogger
        public PCController(ILogger<PCController> logger)
        {
            _logger = logger;
        }

        // Displays all pending claims from the JSON data source
        public IActionResult PendingClaims()
        {
            try
            {
                var claims = LoadClaims(); // Load claim data from JSON
                return View(claims);       // Pass data to the Razor View
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending claims."); // Log detailed error
                TempData["Error"] = "Unable to load pending claims at this time."; // Feedback for user
                return View(new List<Teacher>()); // Return empty view on failure
            }
        }

        // POST: Verifies a specific claim by changing its status to "Verified"
        [HttpPost]
        public IActionResult VerifyClaim(Guid claimId)
        {
            try
            {
                UpdateClaimStatus(claimId, "Verified"); // Update claim state
                TempData["Success"] = "Claim verified successfully!"; // UI feedback message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying claim {ClaimId}", claimId);
                TempData["Error"] = "Failed to verify claim. Please try again.";
            }

            return RedirectToAction("PendingClaims"); // Redirect to refresh view
        }

        // POST: Rejects a specific claim
        [HttpPost]
        public IActionResult RejectClaim(Guid claimId)
        {
            try
            {
                UpdateClaimStatus(claimId, "Rejected"); // Update claim status to Rejected
                TempData["Success"] = "Claim rejected successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim {ClaimId}", claimId);
                TempData["Error"] = "Failed to reject claim. Please try again.";
            }

            return RedirectToAction("PendingClaims");
        }

        // Updates the status of a claim and writes the updated data back to JSON
        private void UpdateClaimStatus(Guid claimId, string newStatus)
        {
            try
            {
                var claims = LoadClaims(); // Load all existing claims
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId); // Locate specific claim

                if (claim != null)
                {
                    claim.Status = newStatus; // Modify claim status
                    var updatedJson = JsonSerializer.Serialize(
                        claims, new JsonSerializerOptions { WriteIndented = true }); // Re-serialize with formatting
                    System.IO.File.WriteAllText(_jsonFile, updatedJson); // Save back to file
                }
                else
                {
                    _logger.LogWarning("Claim {ClaimId} not found.", claimId); // Log missing claim
                    TempData["Warning"] = "Claim not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim status for {ClaimId}", claimId);
                TempData["Error"] = "Failed to update claim status. Please try again.";
            }
        }

        // Securely downloads and decrypts an encrypted document
        public IActionResult DownloadDocument(string fileName)
        {
            try
            {
                var encryptedPath = Path.Combine(_encryptedDocsPath, fileName + ".enc");

                // Ensure file exists before decryption
                if (!System.IO.File.Exists(encryptedPath))
                {
                    TempData["Error"] = "Encrypted document not found.";
                    return RedirectToAction("PendingClaims");
                }

                // Create temporary folder for decrypted documents
                var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
                Directory.CreateDirectory(tempDir);

                var decryptedPath = Path.Combine(tempDir, fileName);
                AESService.DecryptFile(encryptedPath, decryptedPath); // Decrypt using AES encryption service

                var bytes = System.IO.File.ReadAllBytes(decryptedPath); // Read decrypted file into memory
                System.IO.File.Delete(decryptedPath); // Delete decrypted copy for security

                // Return file as a downloadable response
                return File(bytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting or downloading document {FileName}", fileName);
                TempData["Error"] = "Error decrypting or downloading document.";
                return RedirectToAction("PendingClaims");
            }
        }

        // Loads and deserializes claim data from the JSON file
        private List<Teacher> LoadClaims()
        {
            try
            {
                // If file is missing, return an empty list
                if (!System.IO.File.Exists(_jsonFile))
                    return new List<Teacher>();

                var json = System.IO.File.ReadAllText(_jsonFile); // Read raw JSON text
                return JsonSerializer.Deserialize<List<Teacher>>(json) ?? new List<Teacher>(); // Convert JSON to objects
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load claims from JSON file.");
                TempData["Error"] = "Unable to load claims at this time.";
                return new List<Teacher>();
            }
        }
    }
}
