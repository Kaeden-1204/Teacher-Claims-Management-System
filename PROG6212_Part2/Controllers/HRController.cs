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
        private readonly ApplicationDbContext _db;

        public HRController(ApplicationDbContext db)
        {
            _db = db;
        }

        private bool IsHR() => HttpContext.Session.GetString("UserRole") == "HR";

        
        public async Task<IActionResult> TeacherList()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            var teachers = await _db.Users.Where(u => u.Role == "Teacher").ToListAsync();
            return View(teachers);
        }

        public IActionResult CreateTeacher()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher([Bind("FullName,Email,Password,HourlyRate")] User user)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            ModelState.Remove("Role");
            if (!ModelState.IsValid) return View(user);

            user.Role = "Teacher";
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher created successfully!";
            return RedirectToAction("TeacherList");
        }

        public async Task<IActionResult> EditTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);
            if (teacher == null || teacher.Role != "Teacher") return NotFound();

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher([Bind("UserId,FullName,Email,HourlyRate")] User user)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            ModelState.Remove("Role");
            ModelState.Remove("Password");

            if (!ModelState.IsValid) return View(user);

            var existing = await _db.Users.FindAsync(user.UserId);
            if (existing == null || existing.Role != "Teacher") return NotFound();

            existing.FullName = user.FullName;
            existing.Email = user.Email;
            existing.HourlyRate = user.HourlyRate;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Teacher updated successfully!";
            return RedirectToAction("TeacherList");
        }

        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            var teacher = await _db.Users.FindAsync(id);
            if (teacher == null || teacher.Role != "Teacher") return NotFound();
            return View(teacher);
        }

        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            var teacher = await _db.Users.FindAsync(id);
            if (teacher == null || teacher.Role != "Teacher") return NotFound();

            _db.Users.Remove(teacher);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Teacher deleted successfully!";
            return RedirectToAction("TeacherList");
        }

       
        public async Task<IActionResult> HRIndex()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var claims = await _db.Claims
                .Include(c => c.User)
                .Include(c => c.Documents)
                .Where(c => c.Status == "Approved")
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        public IActionResult GenerateInvoice(int id)
        {
            var claim = _db.Claims
                .Include(c => c.User)
                .Include(c => c.Documents)
                .FirstOrDefault(c => c.ClaimId == id);

            if (claim == null) return NotFound();

            byte[] pdfBytes = GenerateInvoicePdf(claim);

            // Return PDF to open in new tab 
            return File(pdfBytes, "application/pdf");
        }


        private byte[] GenerateInvoicePdf(Claim claim)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

                // Title
                doc.Add(new Paragraph("UNIVERSITY PAYMENT INVOICE", titleFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });

                // Claim Info
                doc.Add(new Paragraph($"Invoice #: INV-{claim.ClaimId:D6}", boldFont));
                doc.Add(new Paragraph($"Date: {System.DateTime.Now:dd/MM/yyyy}", normalFont));
                doc.Add(new Paragraph($"Status: {claim.Status}", normalFont));
                doc.Add(new Paragraph(" "));

                // Lecturer Info
                doc.Add(new Paragraph("BILL TO:", boldFont));
                doc.Add(new Paragraph($"Name: {claim.User.FullName}", normalFont));
                doc.Add(new Paragraph($"Email: {claim.User.Email}", normalFont));

                doc.Add(new Paragraph(" "));

                // Table
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

                // Total
                doc.Add(new Paragraph($"TOTAL: R {claim.TotalAmount:N2}", boldFont) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 10 });

                doc.Close();
                return ms.ToArray();
            }
        }
    }
}
