using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PROG6212_Part2.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db; // Database context for interacting with Users and Claims tables

        public HRController(ApplicationDbContext db)
        {
            _db = db; // Inject database context
        }

        // Checks if the logged-in user is HR
        private bool IsHR() => HttpContext.Session.GetString("UserRole") == "HR";

        // Displays a list of all teachers in the system
        public async Task<IActionResult> TeacherList()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home"); // Block non-HR access
            var teachers = await _db.Users.Where(u => u.Role == "Teacher").ToListAsync(); // Get all teachers
            return View(teachers);
        }

        // Shows the Create Teacher form
        public IActionResult CreateTeacher()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            return View();
        }

        // Handles teacher creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher([Bind("FullName,Email,Password,HourlyRate")] User user)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            ModelState.Remove("Role"); // Remove unneeded validation for Role property

            if (!ModelState.IsValid) return View(user); // Return view with validation messages

            user.Role = "Teacher"; // Assign role automatically
            _db.Users.Add(user);   // Add teacher to database
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher created successfully!";
            return RedirectToAction("TeacherList");
        }

        // Loads the edit page for a specific teacher
        public async Task<IActionResult> EditTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id); // Get teacher by ID
            if (teacher == null || teacher.Role != "Teacher") return NotFound(); // Ensure user exists and is a teacher

            return View(teacher);
        }

        // Saves teacher updates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher([Bind("UserId,FullName,Email,HourlyRate")] User user)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            ModelState.Remove("Role");     // Ignore role validation
            ModelState.Remove("Password"); // Password is not editable here

            if (!ModelState.IsValid) return View(user);

            var existing = await _db.Users.FindAsync(user.UserId); // Get current teacher data
            if (existing == null || existing.Role != "Teacher") return NotFound();

            // Update fields
            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.HourlyRate = user.HourlyRate;

            await _db.SaveChangesAsync(); // Save changes to DB
            TempData["Success"] = "Teacher updated successfully!";
            return RedirectToAction("TeacherList");
        }

        // Shows the delete confirmation page
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);
            if (teacher == null || teacher.Role != "Teacher") return NotFound();

            return View(teacher);
        }

        // Confirms deletion of a teacher
        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);
            if (teacher == null || teacher.Role != "Teacher") return NotFound();

            _db.Users.Remove(teacher); // Delete teacher from database
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher deleted successfully!";
            return RedirectToAction("TeacherList");
        }

        // HR dashboard showing all approved claims
        public async Task<IActionResult> HRIndex()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var claims = await _db.Claims
                .Include(c => c.User)      // Include teacher info
                .Include(c => c.Documents) // Include uploaded documents
                .Where(c => c.Status == "Approved")
                .ToListAsync();

            return View(claims);
        }

        // Generates and returns invoice PDF for a claim
        [HttpPost]
        public IActionResult GenerateInvoice(int id)
        {
            var claim = _db.Claims
                .Include(c => c.User)
                .Include(c => c.Documents)
                .FirstOrDefault(c => c.ClaimId == id);

            if (claim == null) return NotFound();

            byte[] pdfBytes = GenerateInvoicePdf(claim); // Build PDF document

            return File(pdfBytes, "application/pdf"); // Return PDF to browser
        }

        // Builds the invoice PDF using iTextSharp
        private byte[] GenerateInvoicePdf(Claim claim)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 50, 50, 25, 25); // Set page margins
                PdfWriter.GetInstance(doc, ms); // Attach writer
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18); // Title font
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);     // Regular font
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);  // Bold font

                // Add invoice title
                doc.Add(new Paragraph("UNIVERSITY PAYMENT INVOICE", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                });

                // Invoice metadata
                doc.Add(new Paragraph($"Invoice #: INV-{claim.ClaimId:D6}", boldFont));
                doc.Add(new Paragraph($"Date: {System.DateTime.Now:dd/MM/yyyy}", normalFont));
                doc.Add(new Paragraph($"Status: {claim.Status}", normalFont));
                doc.Add(new Paragraph(" "));

                // Teacher information
                doc.Add(new Paragraph("BILL TO:", boldFont));
                doc.Add(new Paragraph($"Name: {claim.User.FullName}", normalFont));
                doc.Add(new Paragraph($"Email: {claim.User.Email}", normalFont));
                doc.Add(new Paragraph(" "));

                // Table containing claim details
                PdfPTable table = new PdfPTable(4) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 2, 1, 1, 1 });

                table.AddCell(new PdfPCell(new Phrase("Subject", boldFont)));
                table.AddCell(new PdfPCell(new Phrase("Hours Worked", boldFont)));
                table.AddCell(new PdfPCell(new Phrase("Rate (R)", boldFont)));
                table.AddCell(new PdfPCell(new Phrase("Total (R)", boldFont)));

                table.AddCell(new PdfPCell(new Phrase(claim.Subject, normalFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.HoursWorked.ToString("N2"), normalFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.HourlyRate.ToString("N2"), normalFont)));
                table.AddCell(new PdfPCell(new Phrase(claim.TotalAmount.ToString("N2"), normalFont)));

                doc.Add(table);

                // Total amount displayed at the bottom
                doc.Add(new Paragraph($"TOTAL: R {claim.TotalAmount:N2}", boldFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 10
                });

                doc.Close();
                return ms.ToArray(); // Return PDF bytes
            }
        }
    }
}
/*
References:

1. Microsoft. (2025) 'Controller in ASP.NET Core MVC', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions (Accessed: 21 November 2025).

2. Microsoft. (2025) 'Entity Framework Core Overview', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 21 November 2025).

3. Microsoft. (2025) 'Session state in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#session-state (Accessed: 21 November 2025).

4. iTextSharp. (2025) 'iTextSharp – PDF Library for .NET', iText. Available at: https://itextpdf.com/en/resources/examples/itext-7-dotnet (Accessed: 21 November 2025).

5. Microsoft. (2025) 'ASP.NET Core TempData', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#tempdata (Accessed: 21 November 2025).

6. Microsoft. (2025) 'MemoryStream Class', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.io.memorystream (Accessed: 21 November 2025).
*/
