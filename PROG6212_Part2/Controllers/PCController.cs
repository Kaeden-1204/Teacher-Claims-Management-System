using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class PCController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context for accessing claims and documents

        public PCController(ApplicationDbContext context)
        {
            _context = context; // Injected database context
        }

        // Displays all claims that are still pending verification
        public async Task<IActionResult> PendingClaims()
        {
            var claims = await _context.Claims
                                       .Include(c => c.Documents) // Load associated documents
                                       .Where(c => c.Status == "Pending") // Filter only pending claims
                                       .ToListAsync();
            return View(claims);
        }

        // Marks a claim as "Verified"
        [HttpPost]
        public async Task<IActionResult> VerifyClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId); // Fetch claim by ID
            if (claim != null)
            {
                claim.Status = "Verified"; // Update status
                await _context.SaveChangesAsync(); // Save change to database
                TempData["Success"] = "Claim verified successfully!";
            }
            return RedirectToAction(nameof(PendingClaims)); // Reload pending claims list
        }

        // Marks a claim as "Rejected"
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Rejected"; // Update status
                await _context.SaveChangesAsync();
                TempData["Success"] = "Claim rejected successfully!";
            }
            return RedirectToAction(nameof(PendingClaims));
        }

        // Downloads a document associated with a claim (after decrypting it)
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            // Load document and its related claim
            var document = await _context.ClaimDocuments
                                         .Include(d => d.Claim)
                                         .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(PendingClaims));
            }

            // Construct full path to encrypted file
            var encryptedPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents", document.FilePath);
            if (!System.IO.File.Exists(encryptedPath))
            {
                TempData["Error"] = "Encrypted file not found.";
                return RedirectToAction(nameof(PendingClaims));
            }

            // Create temporary directory for decrypted output
            var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
            Directory.CreateDirectory(tempDir);

            // Path to store decrypted copy
            var decryptedPath = Path.Combine(tempDir, document.FileName);

            try
            {
                // Decrypt the file to temporary location
                AESService.DecryptFile(encryptedPath, decryptedPath);

                // Read decrypted bytes to return to the user
                var bytes = await System.IO.File.ReadAllBytesAsync(decryptedPath);

                // Remove decrypted temporary file
                System.IO.File.Delete(decryptedPath);

                // Return file for download
                return File(bytes, "application/octet-stream", document.FileName);
            }
            catch (Exception ex)
            {
                // Handle any errors during decryption
                TempData["Error"] = "Error decrypting file.";
                return RedirectToAction(nameof(PendingClaims));
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

5. Microsoft. (2025) 'ASP.NET Core TempData', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#tempdata (Accessed: 21 November 2025).
*/

