using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
