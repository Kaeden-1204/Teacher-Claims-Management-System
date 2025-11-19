using Microsoft.AspNetCore.Mvc;

namespace PROG6212_Part2.Controllers
{
    public class HRController : Controller
    {
        public IActionResult HRIndex()
        {
            return View();
        }
    }
}
