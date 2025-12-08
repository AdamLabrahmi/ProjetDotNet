using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class ProjectsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
