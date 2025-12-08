using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class IssuesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
