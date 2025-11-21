using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class AMController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AMController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> VerifiedClaims()
        {
            var claims = await _context.Claims
                                       .Include(c => c.Documents)
                                       .Where(c => c.Status == "Verified")
                                       .ToListAsync();
            return View(claims);
        }

        
        [HttpPost]
        public async Task<IActionResult> ApproveClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Approved";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Claim approved successfully!";
            }
            return RedirectToAction(nameof(VerifiedClaims));
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
            return RedirectToAction(nameof(VerifiedClaims));
        }

        
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var document = await _context.ClaimDocuments.FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(VerifiedClaims));
            }

            
            var encryptedPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents", document.FilePath);
            if (!System.IO.File.Exists(encryptedPath))
            {
                TempData["Error"] = "Encrypted document not found.";
                return RedirectToAction(nameof(VerifiedClaims));
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
            catch
            {
                TempData["Error"] = "Error decrypting document.";
                return RedirectToAction(nameof(VerifiedClaims));
            }
        }

    }
}
