using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Controllers
{
    public class MembreProjetController : Controller
    {
        private readonly AppDbContext _context;

        public MembreProjetController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /MembreProjet/Add?projectId=5
        public IActionResult Add(int projectId)
        {
            ViewBag.ProjectId = projectId;
            return View("~/Views/MembreProjet/AddMember.cshtml");
        }

        // GET: /MembreProjet/SearchUsers?term=foo&projectId=5
        [HttpGet]
        public IActionResult SearchUsers(string term, int projectId)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var users = _context.Utilisateurs
                .Where(u =>
                    (
                        (!string.IsNullOrEmpty(u.Nom) && u.Nom.Contains(term)) ||
                        (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(term))
                    ) &&
                    !_context.MembreProjets.Any(mp =>
                        mp.ProjectID == projectId && mp.UserID == u.UserID
                    )
                )
                .Select(u => new
                {
                    id = u.UserID,
                    text = $"{u.Nom} ({u.Email})"
                })
                .Take(10)
                .ToList();

            return Json(users);
        }

        // POST: /MembreProjet/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMember([FromForm] int projectId, [FromForm] int userId)
        {
            bool alreadyExists = _context.MembreProjets
                .Any(mp => mp.ProjectID == projectId && mp.UserID == userId);

            if (!alreadyExists)
            {
                var membre = new MembreProjet
                {
                    ProjectID = projectId,
                    UserID = userId,
                    Role = RoleProjet.Contributeur,
                    DateAjout = DateTime.Now
                };

                _context.MembreProjets.Add(membre);
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    var inner = ex.InnerException?.Message ?? ex.GetBaseException().Message;
                    // TODO: remplacer par _logger.LogError(ex, "...") si vous avez un ILogger injecté
                    return BadRequest(inner); // renvoie texte simple pour que le JS l'affiche
                }
            }

            return Ok();
        }

        // Ajoutez ces actions au controller existant : Edit (GET/POST) et Delete (POST)
        // GET: MembreProjet/Edit?projectId=1&userId=2
        [HttpGet]
        public IActionResult Edit(int projectId, int userId)
        {
            var membre = _context.MembreProjets
                .Include(mp => mp.Utilisateur)
                .Include(mp => mp.Projet)
                .SingleOrDefault(mp => mp.ProjectID == projectId && mp.UserID == userId);

            if (membre == null) return NotFound();

            // préparer la liste des rôles dans ViewBag si nécessaire
            ViewBag.Roles = Enum.GetValues(typeof(RoleProjet))
                                .Cast<RoleProjet>()
                                .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = r.ToString(), Text = r.ToString() })
                                .ToList();

            return View("~/Views/MembreProjet/Edit.cshtml", membre);
        }

        // POST: MembreProjet/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MembreProjet model)
        {
            if (model == null) return BadRequest();

            var existing = _context.MembreProjets.SingleOrDefault(mp => mp.ProjectID == model.ProjectID && mp.UserID == model.UserID);
            if (existing == null) return NotFound();

            existing.Role = model.Role;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour du membre.");
                ViewBag.Roles = Enum.GetValues(typeof(RoleProjet)).Cast<RoleProjet>()
                                    .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = r.ToString(), Text = r.ToString() })
                                    .ToList();
                return View("~/Views/MembreProjet/Edit.cshtml", model);
            }

            return RedirectToAction("Details", "Projects", new { id = model.ProjectID });
        }

        // POST: MembreProjet/Delete (form classique)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int projectId, int userId)
        {
            var membre = _context.MembreProjets.SingleOrDefault(mp => mp.ProjectID == projectId && mp.UserID == userId);
            if (membre == null) return NotFound();

            _context.MembreProjets.Remove(membre);
            _context.SaveChanges();

            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}
