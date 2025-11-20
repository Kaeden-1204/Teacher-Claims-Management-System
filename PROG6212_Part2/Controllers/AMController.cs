using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class AMController : Controller
    {
        //private readonly ApplicationDbContext _context;
        //private readonly ILogger<AMController> _logger;
        //private readonly string _encryptedDocsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents");

        //public AMController(ApplicationDbContext context, ILogger<AMController> logger)
        //{
        //    _context = context;
        //    _logger = logger;
        //}

        //// Display all verified claims
        public async Task<IActionResult> VerifiedClaims()
        {
            //try
            //{
            //    var claims = await _context.Claims
            //        .Include(c => c.Documents)
            //        .Where(c => c.Status == "Verified")
            //        .ToListAsync();

            //    return View(claims);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error loading verified claims.");
            //    TempData["Error"] = "Unable to load verified claims at this time.";
                return View(VerifiedClaims);
            }
        }

        //// Approve a specific claim
        //[HttpPost]
        //public async Task<IActionResult> ApproveClaim(int claimId)
        //{
        //    try
        //    {
        //        var claim = await _context.Claims.FindAsync(claimId);
        //        if (claim == null)
        //        {
        //            TempData["Warning"] = "Claim not found.";
        //            return RedirectToAction("VerifiedClaims");
        //        }

        //        claim.Status = "Approved";
        //        await _context.SaveChangesAsync();

        //        TempData["Success"] = "Claim approved successfully!";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error approving claim {ClaimId}", claimId);
        //        TempData["Error"] = "Failed to approve claim. Please try again.";
        //    }
        //    return RedirectToAction("VerifiedClaims");
        //}

        //// Reject a specific claim
        //[HttpPost]
        //public async Task<IActionResult> RejectClaim(int claimId)
        //{
        //    try
        //    {
        //        var claim = await _context.Claims.FindAsync(claimId);
        //        if (claim == null)
        //        {
        //            TempData["Warning"] = "Claim not found.";
        //            return RedirectToAction("VerifiedClaims");
        //        }

        //        claim.Status = "Rejected";
        //        await _context.SaveChangesAsync();

        //        TempData["Success"] = "Claim rejected successfully!";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error rejecting claim {ClaimId}", claimId);
        //        TempData["Error"] = "Failed to reject claim. Please try again.";
        //    }
        //    return RedirectToAction("VerifiedClaims");
        //}

        //// Download and decrypt a document
        //public IActionResult DownloadDocument(string fileName)
        //{
        //    try
        //    {
        //        var encryptedPath = Path.Combine(_encryptedDocsPath, fileName + ".enc");
        //        if (!System.IO.File.Exists(encryptedPath))
        //        {
        //            TempData["Error"] = "Encrypted document not found.";
        //            return RedirectToAction("VerifiedClaims");
        //        }

        //        var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
        //        Directory.CreateDirectory(tempDir);

        //        var decryptedPath = Path.Combine(tempDir, fileName);
        //        AESService.DecryptFile(encryptedPath, decryptedPath);

        //        var bytes = System.IO.File.ReadAllBytes(decryptedPath);
        //        System.IO.File.Delete(decryptedPath);

        //        return File(bytes, "application/octet-stream", fileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error decrypting or downloading document {FileName}", fileName);
        //        TempData["Error"] = "Error decrypting or downloading document.";
        //        return RedirectToAction("VerifiedClaims");
        //    }
        //}
    }

