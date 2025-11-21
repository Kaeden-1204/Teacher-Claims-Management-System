using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;


namespace PROG6212_Part2.Controllers
{
    public class AMController : Controller
    {
        private readonly ApplicationDbContext _context;   // Database context for accessing claims and documents

        public AMController(ApplicationDbContext context)
        {
            _context = context;   // Inject database context through constructor
        }

        // Displays all claims that have a status of "Verified"
        public async Task<IActionResult> VerifiedClaims()
        {
            var claims = await _context.Claims
                                       .Include(c => c.Documents)   // Load documents linked to each claim
                                       .Where(c => c.Status == "Verified")   // Filter by verified status
                                       .ToListAsync();   // Execute query asynchronously
            return View(claims);   // Return the list to the view
        }

        // Approves a verified claim by setting its status to "Approved"
        [HttpPost]
        public async Task<IActionResult> ApproveClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);   // Find claim by ID
            if (claim != null)
            {
                claim.Status = "Approved";   // Update claim status
                await _context.SaveChangesAsync();   // Save change to database
                TempData["Success"] = "Claim approved successfully!";   // Temporary message for UI
            }
            return RedirectToAction(nameof(VerifiedClaims));   // Return to verified claims page
        }

        // Rejects a verified claim by updating its status to "Rejected"
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);   // Find claim by ID
            if (claim != null)
            {
                claim.Status = "Rejected";   // Update claim status
                await _context.SaveChangesAsync();   // Save change to database
                TempData["Success"] = "Claim rejected successfully!";   // Notification message
            }
            return RedirectToAction(nameof(VerifiedClaims));   // Back to verified claims view
        }

        // Downloads a decrypted copy of a stored encrypted claim document
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var document = await _context.ClaimDocuments.FirstOrDefaultAsync(d => d.DocumentId == documentId);   // Get document by ID
            if (document == null)
            {
                TempData["Error"] = "Document not found.";   // Error if not found
                return RedirectToAction(nameof(VerifiedClaims));
            }

            // Build full path to encrypted file in the Documents folder
            var encryptedPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents", document.FilePath);
            if (!System.IO.File.Exists(encryptedPath))
            {
                TempData["Error"] = "Encrypted document not found.";   // Error if missing on disk
                return RedirectToAction(nameof(VerifiedClaims));
            }

            // Create temporary folder for decrypted files
            var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
            Directory.CreateDirectory(tempDir);

            // Path where decrypted file will be saved temporarily
            var decryptedPath = Path.Combine(tempDir, document.FileName);

            try
            {
                AESService.DecryptFile(encryptedPath, decryptedPath);   // Decrypt the file

                var bytes = await System.IO.File.ReadAllBytesAsync(decryptedPath);   // Read decrypted file into memory

                System.IO.File.Delete(decryptedPath);   // Delete decrypted temp file for security

                return File(bytes, "application/octet-stream", document.FileName);   // Return file to user for download
            }
            catch
            {
                TempData["Error"] = "Error decrypting document.";   // Error if something goes wrong
                return RedirectToAction(nameof(VerifiedClaims));
            }
        }

    }
}
/*
References:

1. Microsoft. (2025) 'Controller in ASP.NET Core MVC', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions (Accessed: 21 November 2025).

2. Microsoft. (2025) 'Entity Framework Core Overview', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 21 November 2025).

3. Microsoft. (2025) 'Asynchronous programming with async and await in C#', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/ (Accessed: 21 November 2025).

4. Microsoft. (2025) 'File and stream I/O', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/standard/io/ (Accessed: 21 November 2025).

5. Microsoft. (2025) 'TempData in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#tempdata (Accessed: 21 November 2025).
*/
