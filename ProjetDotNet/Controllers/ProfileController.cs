using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjetDotNet.Data;
using ProjetDotNet.Models.ViewModels;
using System.Security.Claims;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Profile
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                UserID = user.UserID,
                Nom = user.Nom ?? "",
                Email = user.Email,
                Telephone = user.Telephone,
                Avatar = user.Avatar
            };

            return View(vm);
        }

        // GET: /Profile/Edit
        public IActionResult Edit()
        {
            return Index(); // même modèle
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Utilisateurs.Find(userId);

            if (user == null) return NotFound();

            user.Nom = model.Nom;
            user.Telephone = model.Telephone;
            user.Avatar = model.Avatar;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
