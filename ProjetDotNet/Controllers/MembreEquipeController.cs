using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Controllers;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;


namespace ProjetDotNet.Controllers
{
    public class MembreEquipeController : Controller
    {
        private readonly AppDbContext _context;

        public MembreEquipeController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return AuthorizationHelper.GetCurrentUserId(User, _context);
        }

        // 1️⃣ Page Ajouter membre (GET)
        public IActionResult Add(int teamId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Challenge();

            if (!AuthorizationHelper.CanAddMembersToTeam(_context, current, teamId))
                return Forbid();

            ViewBag.TeamId = teamId;
            return View();
        }

        // 2️⃣ Recherche AJAX (nom ou email)
        [HttpGet]
        public IActionResult SearchUsers(string term, int teamId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanAddMembersToTeam(_context, current, teamId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var users = _context.Utilisateurs
                .Where(u =>
                    (
                        (!string.IsNullOrEmpty(u.Nom) && u.Nom.Contains(term)) ||
                        (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(term))
                    ) &&
                    !_context.MembreEquipes.Any(me =>
                        me.TeamID == teamId && me.UserID == u.UserID
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

        // 3️⃣ Ajouter un membre existant (POST)
        [HttpPost]
        public IActionResult AddMember(int teamId, int userId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanAddMembersToTeam(_context, current, teamId))
            {
                return Forbid();
            }

            bool alreadyExists = _context.MembreEquipes
                .Any(me => me.TeamID == teamId && me.UserID == userId);

            if (!alreadyExists)
            {
                var membre = new MembreEquipe
                {
                    TeamID = teamId,
                    UserID = userId,
                    Role = RoleEquipe.Membre
                };

                _context.MembreEquipes.Add(membre);
                _context.SaveChanges();
            }

            return Ok();
        }

        // GET: MembreEquipe/Edit?teamId=1&userId=2
        [HttpGet]
        public IActionResult Edit(int teamId, int userId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Challenge();

            if (!AuthorizationHelper.CanManageTeamMembers(_context, current, teamId))
            {
                return Forbid();
            }

            var membre = _context.MembreEquipes
                .Include(m => m.Utilisateur)
                .Include(m => m.Equipe)
                .SingleOrDefault(m => m.TeamID == teamId && m.UserID == userId);

            if (membre == null)
                return NotFound();

            if (membre.Utilisateur == null)
                membre.Utilisateur = _context.Utilisateurs.Find(membre.UserID);

            ViewBag.Roles = Enum.GetValues(typeof(RoleEquipe))
                                .Cast<RoleEquipe>()
                                .Select(r => new SelectListItem { Value = r.ToString(), Text = r.ToString() })
                                .ToList();

            ViewBag.TeamId = teamId;
            return View("~/Views/MembreEquipe/Edit.cshtml", membre);
        }

        // POST: MembreEquipe/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MembreEquipe model)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanManageTeamMembers(_context, current, model.TeamID))
            {
                return Forbid();
            }

            if (model == null)
                return BadRequest();

            var existing = _context.MembreEquipes
                .SingleOrDefault(m => m.TeamID == model.TeamID && m.UserID == model.UserID);

            if (existing == null)
                return NotFound();

            existing.Role = model.Role;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ViewBag.Roles = Enum.GetValues(typeof(RoleEquipe))
                                    .Cast<RoleEquipe>()
                                    .Select(r => new SelectListItem { Value = r.ToString(), Text = r.ToString() })
                                    .ToList();

                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour du membre.");
                ViewBag.TeamId = model.TeamID;
                return View("~/Views/MembreEquipe/Edit.cshtml", model);
            }

            return RedirectToAction("Details", "Equipes", new { id = model.TeamID });
        }

        // GET: MembreEquipe/Delete?teamId=1&userId=2
        [HttpGet]
        public IActionResult Delete(int teamId, int userId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanManageTeamMembers(_context, current, teamId))
            {
                return Forbid();
            }

            var membre = _context.MembreEquipes
                .Include(m => m.Utilisateur)
                .SingleOrDefault(m => m.TeamID == teamId && m.UserID == userId);

            if (membre == null)
                return NotFound();

            ViewBag.TeamId = teamId;
            return View("~/Views/MembreEquipe/Delete.cshtml", membre);
        }

        // POST: MembreEquipe/Delete (form classique)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int teamId, int userId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanManageTeamMembers(_context, current, teamId))
            {
                return Forbid();
            }

            var membre = _context.MembreEquipes
                .SingleOrDefault(m => m.TeamID == teamId && m.UserID == userId);

            if (membre == null)
                return NotFound();

            _context.MembreEquipes.Remove(membre);
            _context.SaveChanges();

            return RedirectToAction("Details", "Equipes", new { id = teamId });
        }

        // POST: MembreEquipe/RemoveMember (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveMember([FromForm] int teamId, [FromForm] int userId)
        {
            var current = GetCurrentUserId();
            if (current == 0) return Forbid();

            if (!AuthorizationHelper.CanManageTeamMembers(_context, current, teamId))
            {
                return Forbid();
            }

            var membre = _context.MembreEquipes
                .SingleOrDefault(m => m.TeamID == teamId && m.UserID == userId);

            if (membre == null)
                return NotFound();

            _context.MembreEquipes.Remove(membre);
            _context.SaveChanges();

            return Ok();
        }
    }
}