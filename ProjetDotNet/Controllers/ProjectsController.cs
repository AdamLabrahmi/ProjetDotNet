using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(AppDbContext context, ILogger<ProjectsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Projects
        //public IActionResult Index()
        //{
        //    var projets = _context.Projets
        //        .Include(p => p.Organisation)
        //        .OrderByDescending(p => p.DateCreation)
        //        .ToList();
        //    return View("~/Views/Projects/Index.cshtml", projets);
        //}


        public IActionResult Index()
        {
            // récupérer user courant + droit site admin
            var current = AuthorizationHelper.GetCurrentUserId(User, _context);
            var isSiteAdmin = AuthorizationHelper.IsSiteAdmin(_context, current);

            // requête de base incluant organisation et membres/utilisateurs
            var query = _context.Projets
                .AsNoTracking()
                .Include(p => p.Organisation)
                .Include(p => p.Membres)
                    .ThenInclude(mp => mp.Utilisateur)
                .AsQueryable();

            if (!isSiteAdmin)
            {
                // ne conserver que les projets où l'utilisateur est membre
                query = query.Where(p => p.Membres.Any(mp => mp.UserID == current));
            }

            var projets = query
                .OrderByDescending(p => p.DateCreation)
                .ToList();

            if (!isSiteAdmin && (projets == null || projets.Count == 0))
            {
                ViewBag.InfoMessage = "Vous n'êtes affecté à aucun projet pour le moment.";
            }

            return View("~/Views/Projects/Index.cshtml", projets);
        }



        //public IActionResult Details(int id)
        //{
        //    var projet = _context.Projets
        //        .AsNoTracking()
        //        .Include(p => p.Organisation)
        //        .Include(p => p.Membres!)
        //            .ThenInclude(mp => mp.Utilisateur)
        //        .Include(p => p.Sprints)
        //        .Include(p => p.Taches)
        //            .ThenInclude(t => t.Assignee)
        //        .FirstOrDefault(p => p.ProjectID == id);

        //    if (projet == null) return NotFound();

        //    return View("~/Views/Projects/Details.cshtml", projet);
        //}



        public IActionResult Details(int id)
        {
            var tache = _context.Taches
                .AsNoTracking()
                .Include(t => t.Projet)
                .Include(t => t.Sprint)
                .Include(t => t.Assignee)
                .Include(t => t.Createur)
                .FirstOrDefault(t => t.TacheID == id);

            if (tache == null) return NotFound();
            return View("~/Views/Taches/Details.cshtml", tache);
        }



        // GET: /Projects/Create
        // GET Create
        public IActionResult Create()
        {
            int current = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (current == 0) return Challenge();

            // Autorisé si SiteAdmin OU Admin d'au moins une équipe
            bool allowed = AuthorizationHelper.IsSiteAdmin(_context, current) || AuthorizationHelper.IsAnyTeamAdmin(_context, current);
            if (!allowed) return Forbid();

            PopulateOrganisationDropDown();
            PopulateStatutDropDown();
            var model = new Projet { DateDebut = DateTime.Today, DateFin = DateTime.Today.AddMonths(3) };
            return View("~/Views/Projects/Create.cshtml", model);
        }

        // POST: /Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Projet projet)
        {
            PopulateOrganisationDropDown(projet.OrgID);
            PopulateStatutDropDown(projet.Statut);

            if (!ModelState.IsValid)
                return View("~/Views/Projects/Create.cshtml", projet);

            if (string.IsNullOrWhiteSpace(projet.CleProjet))
                projet.CleProjet = GenerateProjectKey(projet.Nom);

            projet.DateCreation = DateTime.Now;

            try
            {
                _context.Projets.Add(projet);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du projet");
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la sauvegarde.");
                return View("~/Views/Projects/Create.cshtml", projet);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Projects/Edit/5
        public IActionResult Edit(int id)
        {
            var projet = _context.Projets.Find(id);
            if (projet == null) return NotFound();

            PopulateOrganisationDropDown(projet.OrgID);
            PopulateStatutDropDown(projet.Statut);
            return View("~/Views/Projects/Edit.cshtml", projet);
        }

        // POST: /Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Projet updated)
        {
            if (id != updated.ProjectID) return BadRequest();

            PopulateOrganisationDropDown(updated.OrgID);
            PopulateStatutDropDown(updated.Statut);

            if (!ModelState.IsValid)
                return View("~/Views/Projects/Edit.cshtml", updated);

            var existing = _context.Projets.FirstOrDefault(p => p.ProjectID == id);
            if (existing == null) return NotFound();

            existing.Nom = updated.Nom;
            existing.Description = updated.Description;
            existing.DateDebut = updated.DateDebut;
            existing.DateFin = updated.DateFin;
            existing.Statut = updated.Statut;
            existing.OrgID = updated.OrgID;
            existing.CleProjet = string.IsNullOrWhiteSpace(updated.CleProjet) ? existing.CleProjet : updated.CleProjet;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du projet ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour.");
                return View("~/Views/Projects/Edit.cshtml", updated);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Projects/Delete/5
        public IActionResult Delete(int id)
        {
            var projet = _context.Projets
                .Include(p => p.Organisation)
                .FirstOrDefault(p => p.ProjectID == id);
            if (projet == null) return NotFound();
            return View("~/Views/Projects/Delete.cshtml", projet);
        }

        // POST: /Projects/Delete/5
        // POST DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            int current = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (current == 0) return Challenge();

            bool allowed = AuthorizationHelper.IsSiteAdmin(_context, current) || AuthorizationHelper.IsAnyTeamAdmin(_context, current);
            if (!allowed) return Forbid();

            var projet = _context.Projets.Find(id);
            if (projet == null) return NotFound();

            try
            {
                _context.Projets.Remove(projet);
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du projet ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Impossible de supprimer le projet (contraintes DB).");
                return View("~/Views/Projects/Delete.cshtml", projet);
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateOrganisationDropDown(object selected = null)
        {
            ViewBag.Organisations = _context.Organisations
                .AsNoTracking()
                .OrderBy(o => o.Nom)
                .Select(o => new SelectListItem { Value = o.OrgID.ToString(), Text = o.Nom })
                .ToList();
        }

        private void PopulateStatutDropDown(object selected = null)
        {
            var items = Enum.GetValues(typeof(StatutProjet))
                            .Cast<StatutProjet>()
                            .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
                            .ToList();
            ViewBag.Statuts = items;
        }

        private static string GenerateProjectKey(string orgName)
        {
            if (string.IsNullOrWhiteSpace(orgName))
                orgName = "PRJ";
            var prefix = new string(orgName.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
            return $"{prefix}-{suffix}";
        }
    }
}