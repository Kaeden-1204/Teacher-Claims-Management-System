using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class PCController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PCController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public async Task<IActionResult> PendingClaims()
        {
            var claims = await _context.Claims
                                       .Include(c => c.Documents)
                                       .Where(c => c.Status == "Pending")
                                       .ToListAsync();
            return View(claims);
        }

       
        [HttpPost]
        public async Task<IActionResult> VerifyClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Verified";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Claim verified successfully!";
            }
            return RedirectToAction(nameof(PendingClaims));
        }

        
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Claim rejected successfully!";
            }
            return RedirectToAction(nameof(PendingClaims));
        }

        
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            
            var document = await _context.ClaimDocuments
                                         .Include(d => d.Claim)
                                         .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(PendingClaims));
            }

           
            var encryptedPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents", document.FilePath);
            if (!System.IO.File.Exists(encryptedPath))
            {
                TempData["Error"] = "Encrypted file not found.";
                return RedirectToAction(nameof(PendingClaims));
            }

            
            var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
            Directory.CreateDirectory(tempDir);

            var decryptedPath = Path.Combine(tempDir, document.FileName);

            try
            {
                AESService.DecryptFile(encryptedPath, decryptedPath);
                var bytes = await System.IO.File.ReadAllBytesAsync(decryptedPath);
                System.IO.File.Delete(decryptedPath);
                return File(bytes, "application/octet-stream", document.FileName);
            }
            catch (Exception ex)
            {
                
                TempData["Error"] = "Error decrypting file.";
                return RedirectToAction(nameof(PendingClaims));
            }
        }

    }
}
