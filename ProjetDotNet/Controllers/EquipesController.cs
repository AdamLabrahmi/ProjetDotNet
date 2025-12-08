using Microsoft.AspNetCore.Mvc;

namespace ProjetDotNet.Controllers
{
    public class EquipesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
