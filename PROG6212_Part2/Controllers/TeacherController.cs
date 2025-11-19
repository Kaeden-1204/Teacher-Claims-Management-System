using Microsoft.AspNetCore.Mvc;     
using PROG6212_Part2.Models;       
using PROG6212_Part2.Services;    
using System.Text.Json;

namespace PROG6212_Part2.Controllers
{
    public class TeacherController : Controller
    {
        // Directory paths for encrypted documents and stored JSON claim data
        private readonly string _encryptedDocsPath;
        private readonly string _jsonFile;

        // ASP.NET Core's logging interface for structured logging
        private readonly ILogger<TeacherController> _logger;

        // Constructor uses Dependency Injection to initialize ILogger
        public TeacherController(ILogger<TeacherController> logger)
        {
            _logger = logger;//Code Attribution (Reppen, 2016)
            _encryptedDocsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
            _jsonFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "claims.json");

            // Ensure necessary directories exist, creating them if missing
            try
            {
                if (!Directory.Exists(_encryptedDocsPath))
                    Directory.CreateDirectory(_encryptedDocsPath);

                var dataDir = Path.GetDirectoryName(_jsonFile);
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize required directories.");
            }
        }

        // Displays the claim submission form view
        public IActionResult SubmitClaim()
        {
            try
            {
                return View(new Claim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading SubmitClaim view.");
                TempData["Error"] = "An unexpected error occurred while loading the form.";
                return RedirectToAction("ViewClaims");
            }
        }

        // POST handler for submitting or calculating claims
        [HttpPost]
        [ValidateAntiForgeryToken] // Protects against CSRF attacks
        public IActionResult SubmitClaim(Claim model, string action)
        {
            try
            {
                // Allows user to calculate totals without submitting
                if (action == "CalculateTotal")
                    return View(model);

                // Validate input fields before saving
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please correct the highlighted errors and try again.";
                    return View(model);
                }

                var savedFiles = new List<string>();

                // Handle file uploads and encrypt them
                if (model.SupportingDocuments != null)
                {
                    foreach (var file in model.SupportingDocuments)
                    {
                        try
                        {
                            // Restrict file types for security
                            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                            var extension = Path.GetExtension(file.FileName).ToLower();

                            if (!allowedExtensions.Contains(extension))
                            {
                                ModelState.AddModelError("SupportingDocuments", $"File type {extension} is not allowed.");
                                return View(model);
                            }

                            // Restrict file size to 5MB
                            if (file.Length > 5 * 1024 * 1024)
                            {
                                ModelState.AddModelError("SupportingDocuments", "File size cannot exceed 5MB.");
                                return View(model);
                            }

                            // Save and encrypt uploaded file
                            var originalFileName = Path.GetFileName(file.FileName);
                            var tempPath = Path.Combine(_encryptedDocsPath, originalFileName);

                            using (var stream = new FileStream(tempPath, FileMode.Create))
                                file.CopyTo(stream);

                            var encryptedPath = tempPath + ".enc";
                            AESService.EncryptFile(tempPath, encryptedPath); // Custom encryption service
                            System.IO.File.Delete(tempPath); // Delete unencrypted version for security

                            savedFiles.Add(originalFileName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error encrypting or saving file {FileName}", file.FileName);
                            ModelState.AddModelError("SupportingDocuments", $"There was a problem processing {file.FileName}. Please try again.");
                            return View(model);
                        }
                    }
                }

                // Store file names and reset document list before serialization
                model.SavedFiles = savedFiles;
                model.SupportingDocuments = null;

                var claims = new List<Claim>();

                // Load existing claims from JSON file
                try
                {
                    if (System.IO.File.Exists(_jsonFile))
                    {
                        var json = System.IO.File.ReadAllText(_jsonFile);
                        claims = JsonSerializer.Deserialize<List<Claim>>(json) ?? new List<Claim>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read existing claims data. A new file will be created.");
                }

                // Add new claim to list and save back to JSON
                claims.Add(model);

                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(claims, options);
                    System.IO.File.WriteAllText(_jsonFile, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing claim data to file.");
                    TempData["Error"] = "Failed to save your claim. Please try again later.";
                    return View(model);
                }

                TempData["Success"] = "Claim submitted successfully!";//Code Attribution(farshid jahanmanesh, 2019)
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during claim submission.");
                TempData["Error"] = "An unexpected error occurred while submitting your claim. Please try again later.";
                return View(model);
            }
        }

        // Displays all submitted claims with optional search functionality
        public IActionResult ViewClaims(string search)
        {
            try
            {
                var claims = new List<Claim>();

                try
                {
                    if (System.IO.File.Exists(_jsonFile))
                    {
                        var json = System.IO.File.ReadAllText(_jsonFile);
                        claims = JsonSerializer.Deserialize<List<Claim>>(json) ?? new List<Claim>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load claims from JSON file.");
                    TempData["Error"] = "Unable to load previous claims at this time.";
                }

                // Simple search by name or email
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    claims = claims
                        .Where(c => c.FullName.ToLower().Contains(search) || c.Email.ToLower().Contains(search))
                        .ToList();
                }

                return View(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading ViewClaims page.");
                TempData["Error"] = "An unexpected error occurred while loading claims.";
                return View(new List<Claim>());
            }
        }
    }
}


//Reference list
//Advanced C# Programming Course. (2024). freeCodeCamp.org. Available at: https://youtu.be/YT8s-90oDC0 [Accessed 19 Oct. 2025].
//dotnet-bot (2025a). Aes Class (System.Security.Cryptography). [online] Microsoft.com. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-9.0 [Accessed 19 Oct. 2025].
//dotnet-bot (2025b). Guid.NewGuid Method (System). [online] Microsoft.com. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-9.0 [Accessed 19 Oct. 2025].
//dotnet-bot (2025c). System.IO Namespace. [online] Microsoft.com. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.io?view=net-9.0 [Accessed 19 Oct. 2025].
//farshid jahanmanesh (2019). how do i use temp data in asp.net mvc core. [online] Stack Overflow. Available at: https://stackoverflow.com/questions/57072523/how-do-i-use-temp-data-in-asp-net-mvc-core [Accessed 19 Oct. 2025].
//Reppen, B. (2016). How do I log from other classes than the controller in ASP.NET Core? [online] Stack Overflow. Available at: https://stackoverflow.com/questions/39031585/how-do-i-log-from-other-classes-than-the-controller-in-asp-net-core [Accessed 19 Oct. 2025].
//Tutorials, D.N. (2024). TempData in ASP.NET Core MVC. [online] Dot Net Tutorials. Available at: https://dotnettutorials.net/lesson/tempdata-in-asp-net-core-mvc/ [Accessed 19 Oct. 2025].
