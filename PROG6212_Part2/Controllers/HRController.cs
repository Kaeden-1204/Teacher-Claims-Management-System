using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG6212_Part2.Data;
using PROG6212_Part2.Models;

namespace PROG6212_Part2.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HRController(ApplicationDbContext db)
        {
            _db = db;
        }

        private bool IsHR()
        {
            return HttpContext.Session.GetString("UserRole") == "HR";
        }

        public IActionResult HRIndex()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");
            return View();
        }

        public async Task<IActionResult> TeacherList()
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teachers = await _db.Users
                .Where(u => u.Role == "Teacher")
                .ToListAsync();

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

            // Prevent Role from causing validation errors
            ModelState.Remove("Role");

            if (!ModelState.IsValid)
                return View(user);

            user.Role = "Teacher"; // Automatically assign teacher role

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher created successfully!";
            return RedirectToAction("TeacherList");
        }

        public async Task<IActionResult> EditTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);

            if (teacher == null || teacher.Role != "Teacher")
                return NotFound();

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher([Bind("UserId,FullName,Email,HourlyRate")] User user)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            ModelState.Remove("Role");
            ModelState.Remove("Password"); 

            if (!ModelState.IsValid)
                return View(user);

            var existingTeacher = await _db.Users.FindAsync(user.UserId);
            if (existingTeacher == null || existingTeacher.Role != "Teacher")
                return NotFound();

            // Update allowed fields ONLY
            existingTeacher.FullName = user.FullName;
            existingTeacher.Email = user.Email;
            existingTeacher.HourlyRate = user.HourlyRate;

            // DO NOT change password here

            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher updated successfully!";
            return RedirectToAction("TeacherList");
        }

        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);

            if (teacher == null || teacher.Role != "Teacher")
                return NotFound();

            return View(teacher);
        }

        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            if (!IsHR()) return RedirectToAction("AccessDenied", "Home");

            var teacher = await _db.Users.FindAsync(id);

            if (teacher == null || teacher.Role != "Teacher")
                return NotFound();

            _db.Users.Remove(teacher);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher deleted successfully!";
            return RedirectToAction("TeacherList");
        }

    }
}
