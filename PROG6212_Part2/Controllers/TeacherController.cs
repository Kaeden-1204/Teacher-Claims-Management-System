using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TeacherController> _logger;
        private readonly string _documentsPath;

        public TeacherController(ApplicationDbContext db, ILogger<TeacherController> logger)
        {
            _db = db;
            _logger = logger;

            _documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
            if (!Directory.Exists(_documentsPath))
                Directory.CreateDirectory(_documentsPath);
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId");

     
        public async Task<IActionResult> SubmitClaim()
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Home");

            Claim model;

          
            if (TempData["ClaimModel"] != null)
            {
                var json = TempData["ClaimModel"].ToString();
                model = System.Text.Json.JsonSerializer.Deserialize<Claim>(json)!;
            }
            else
            {
                model = new Claim
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    HourlyRate = user.HourlyRate ?? 0,
                    ClaimDate = DateTime.UtcNow.Date
                };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim model, List<IFormFile>? UploadFiles, string? action)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Home");

            // Enforce DB HourlyRate
            model.HourlyRate = user.HourlyRate ?? 0;

            // Calculate total amount
            model.TotalAmount = model.HoursWorked * model.HourlyRate;

            // Just recalc total without saving
            if (action == "CalculateTotal")
            {
                TempData["ClaimModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                return View(model);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(model.Subject))
                ModelState.AddModelError(nameof(model.Subject), "Subject is required.");

            if (!ModelState.IsValid)
                return View(model);

            model.UserId = user.UserId;
            model.Status = "Pending";

          
            if (model.Documents == null)
                model.Documents = new List<ClaimDocument>();

          
            if (UploadFiles != null && UploadFiles.Count > 0)
            {
                foreach (var file in UploadFiles)
                {
                    try
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var allowed = new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".jpg", ".jpeg", ".png" };
                        if (!allowed.Contains(ext))
                        {
                            ModelState.AddModelError("Documents", $"File type {ext} not allowed.");
                            return View(model);
                        }

                        if (file.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("Documents", "File size cannot exceed 5MB.");
                            return View(model);
                        }

                        var safeFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(_documentsPath, safeFileName);

                        // Save temporary file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        // Encrypt file in place
                        var encryptedFilePath = filePath + ".enc";
                        AESService.EncryptFile(filePath, encryptedFilePath);
                        System.IO.File.Delete(filePath); // delete unencrypted file

                        // Save metadata to DB
                        var docRecord = new ClaimDocument
                        {
                            FileName = file.FileName,
                            FilePath = Path.GetFileName(encryptedFilePath),
                            Claim = model
                        };

                        model.Documents.Add(docRecord);
                        _db.ClaimDocuments.Add(docRecord);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file {File}", file.FileName);
                        ModelState.AddModelError("Documents", $"Failed to process {file.FileName}.");
                        return View(model);
                    }
                }
            }

            // Save claim + documents
            _db.Claims.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction(nameof(ViewClaims));
        }

        public async Task<IActionResult> ViewClaims(string? search)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var claimsQuery = _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                claimsQuery = claimsQuery.Where(c =>
                    c.FullName.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s) ||
                    c.Subject.ToLower().Contains(s));
            }

            var claims = await claimsQuery
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();

            return View(claims);
        }

  
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var doc = await _db.ClaimDocuments.Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (doc == null) return NotFound();
            if (doc.Claim.UserId != userId.Value && HttpContext.Session.GetString("UserRole") != "HR")
                return Forbid();

            var encryptedPath = Path.Combine(_documentsPath, doc.FilePath);
            if (!System.IO.File.Exists(encryptedPath)) return NotFound();

            var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs");
            Directory.CreateDirectory(tempDir);
            var decryptedFilePath = Path.Combine(tempDir, doc.FileName);

            try
            {
                AESService.DecryptFile(encryptedPath, decryptedFilePath);
                var bytes = await System.IO.File.ReadAllBytesAsync(decryptedFilePath);
                System.IO.File.Delete(decryptedFilePath);
                return File(bytes, "application/octet-stream", doc.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting document {DocId}", documentId);
                TempData["Error"] = "Error decrypting document.";
                return RedirectToAction(nameof(ViewClaims));
            }
        }
    }
}
