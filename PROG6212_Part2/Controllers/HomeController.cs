using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PROG6212_Part2.Models;
using PROG6212_Part2.Data;

namespace PROG6212_Part2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

            
            return user.Role switch
            {
                "HR" => RedirectToAction("HRIndex", "HR"),
                "PC" => RedirectToAction("Index", "ProgrammeCoordinator"),
                "AM" => RedirectToAction("Index", "AcademicManager"),
                "Teacher" => RedirectToAction("Index", "Teacher"),
                _ => RedirectToAction("Index")
            };
        }
        public IActionResult Logout()
        {
            
            HttpContext.Session.Clear();

           
            return RedirectToAction("Login", "Home");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
