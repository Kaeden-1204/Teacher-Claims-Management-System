using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using PROG6212_Part2.Services;

namespace PROG6212_Part2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _db; // Database context for accessing data
        private readonly ILogger<TeacherController> _logger; // Logger for capturing errors and events
        private readonly string _documentsPath; // Path where encrypted documents are stored

        public TeacherController(ApplicationDbContext db, ILogger<TeacherController> logger)
        {
            _db = db;
            _logger = logger;

            _documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents"); // Build documents directory path
            if (!Directory.Exists(_documentsPath))
                Directory.CreateDirectory(_documentsPath); // Ensure folder exists
        }

        private int? CurrentUserId() => HttpContext.Session.GetInt32("UserId"); // Get logged-in user ID from session

        // Load the claim submission form
        public async Task<IActionResult> SubmitClaim()
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Home");

            Claim model;

            if (TempData["ClaimModel"] != null)
            {
                // Reload the model from TempData
                var json = TempData["ClaimModel"].ToString();
                model = System.Text.Json.JsonSerializer.Deserialize<Claim>(json)!;

                // Ensure ClaimDate is not lost if TempData somehow cleared it
                if (model.ClaimDate == default)
                    model.ClaimDate = DateTime.UtcNow.Date;
            }
            else
            {
                // Default model pre-filled with user data
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


        // Handles form submission when teacher submits a claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(Claim model, List<IFormFile>? UploadFiles, string? action)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Home");

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Home");

            // Force hourly rate from DB
            model.HourlyRate = user.HourlyRate ?? 0;

            // Calculate total
            model.TotalAmount = model.HoursWorked * model.HourlyRate;


            if (model.ClaimDate == default)
                model.ClaimDate = DateTime.UtcNow.Date;

            // If user clicked "Calculate Total", save model in TempData and reload view
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

            // Link claim to user and set initial status
            model.UserId = user.UserId;
            model.Status = "Pending";

            if (model.Documents == null)
                model.Documents = new List<ClaimDocument>();

            // Handle uploaded documents
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

                        // Save file temporarily before encryption
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        // Encrypt the file
                        var encryptedFilePath = filePath + ".enc";
                        AESService.EncryptFile(filePath, encryptedFilePath);
                        System.IO.File.Delete(filePath);

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

            // Save claim and documents
            _db.Claims.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction(nameof(ViewClaims));
        }

        // Shows the logged-in teacher their submitted claims
        public async Task<IActionResult> ViewClaims(string? search)
        {
            var userId = CurrentUserId(); // Ensure user is logged in
            if (userId == null) return RedirectToAction("Login", "Home");

            var claimsQuery = _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.UserId == userId.Value); // Only their claims

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                claimsQuery = claimsQuery.Where(c =>
                    c.FullName.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s) ||
                    c.Subject.ToLower().Contains(s));
            }

            var claims = await claimsQuery
                .OrderByDescending(c => c.ClaimDate) // Latest first
                .ToListAsync();

            return View(claims); // Return claim list
        }

        // Allows user to download their uploaded encrypted documents
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var userId = CurrentUserId(); // Validate login
            if (userId == null) return RedirectToAction("Login", "Home");

            var doc = await _db.ClaimDocuments.Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId); // Find document

            if (doc == null) return NotFound();

            // Ensure only the owner or HR can download
            if (doc.Claim.UserId != userId.Value && HttpContext.Session.GetString("UserRole") != "HR")
                return Forbid();

            var encryptedPath = Path.Combine(_documentsPath, doc.FilePath); // Path to encrypted file
            if (!System.IO.File.Exists(encryptedPath)) return NotFound();

            var tempDir = Path.Combine(Path.GetTempPath(), "DecryptedDocs"); // Temp folder for decrypted file
            Directory.CreateDirectory(tempDir);
            var decryptedFilePath = Path.Combine(tempDir, doc.FileName);

            try
            {
                AESService.DecryptFile(encryptedPath, decryptedFilePath); // Decrypt temp copy
                var bytes = await System.IO.File.ReadAllBytesAsync(decryptedFilePath); // Read decrypted file
                System.IO.File.Delete(decryptedFilePath); // Delete temp decrypted file
                return File(bytes, "application/octet-stream", doc.FileName); // Return file
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting document {DocId}", documentId); // Log error
                TempData["Error"] = "Error decrypting document."; // User-facing error
                return RedirectToAction(nameof(ViewClaims));
            }
        }
    }
}

/*
References:

1. Microsoft. (2025) 'Controller in ASP.NET Core MVC', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions (Accessed: 21 November 2025).

2. Microsoft. (2025) 'Entity Framework Core Overview', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 21 November 2025).

3. Microsoft. (2025) 'Working with files in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads (Accessed: 21 November 2025).

4. Microsoft. (2025) 'Session state in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#session-state (Accessed: 21 November 2025).

5. Microsoft. (2025) 'TempData in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#tempdata (Accessed: 21 November 2025).

6. Microsoft. (2025) 'ILogger Interface', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger (Accessed: 21 November 2025).

7. Microsoft. (2025) 'File and stream I/O', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/standard/io/ (Accessed: 21 November 2025).
*/

