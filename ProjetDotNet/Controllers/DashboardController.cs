using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
