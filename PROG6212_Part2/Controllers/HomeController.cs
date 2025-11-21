using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PROG6212_Part2.Models;
using PROG6212_Part2.Data;

namespace PROG6212_Part2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;   // Logger for tracking errors and system events
        private readonly ApplicationDbContext _context;      // Database context for retrieving user data

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;    // Inject logger
            _context = context;  // Inject database context
        }

        // Default landing page
        public IActionResult Index()
        {
            return View();
        }

        // Displays the login page
        public IActionResult Login()
        {
            return View();
        }

        // Processes login form submission
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Look for a matching user in the database by email and password
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";   // Error message if login fails
                return View();
            }

            // Store user info in session for later use across the system
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetInt32("UserId", user.UserId);

            // Redirect the user based on their assigned role
            return user.Role switch
            {
                "HR" => RedirectToAction("HRIndex", "HR"),                 // HR homepage
                "PC" => RedirectToAction("PendingClaims", "PC"),           // Processing Clerk dashboard
                "AM" => RedirectToAction("VerifiedClaims", "AM"),          // Audit Manager dashboard
                "Teacher" => RedirectToAction("viewClaims", "Teacher"),    // Teacher dashboard
                _ => RedirectToAction("Index")                             // Default fallback
            };
        }

        // Logs the user out by clearing the session
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();   // Remove all stored session values
            return RedirectToAction("Login", "Home");   // Redirect back to login page
        }

        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Displays application error details
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier   // Tracks the request ID for debugging
            });
        }
    }
}
/*
References:

1. Microsoft. (2025) 'Controller in ASP.NET Core MVC', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions (Accessed: 21 November 2025).

2. Microsoft. (2025) 'Working with Entity Framework Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 21 November 2025).

3. Microsoft. (2025) 'Session state in ASP.NET Core', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-8.0#session-state (Accessed: 21 November 2025).

4. Microsoft. (2025) 'ILogger Interface', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger (Accessed: 21 November 2025).

5. Microsoft. (2025) 'ResponseCache Attribute', Microsoft Docs. Available at: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response (Accessed: 21 November 2025).
*/
