using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View(); 
        }

    }
}
