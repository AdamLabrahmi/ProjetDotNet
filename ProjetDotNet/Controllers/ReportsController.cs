using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
