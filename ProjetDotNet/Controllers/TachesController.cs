using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class TachesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TachesController> _logger;

        public TachesController(AppDbContext context, ILogger<TachesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Taches
        public IActionResult Index()
        {
            var taches = _context.Taches
                .Include(t => t.Projet)
                .Include(t => t.Sprint)
                .Include(t => t.Assignee)
                .Include(t => t.Createur)
                .OrderByDescending(t => t.DateCreation)
                .ToList();

            return View("~/Views/Taches/Index.cshtml", taches);
        }

        // GET: /Taches/Details/5
        public IActionResult Details(int id)
        {
            var tache = _context.Taches
                .Include(t => t.Projet)
                .Include(t => t.Sprint)
                .Include(t => t.Assignee)
                .Include(t => t.Createur)
                .FirstOrDefault(t => t.TacheID == id);

            if (tache == null) return NotFound();
            return View("~/Views/Taches/Details.cshtml", tache);
        }

        // GET: /Taches/Create
        public IActionResult Create()
        {
            PopulateProjectsDropDown();
            PopulateSprintsDropDown();
            PopulateUsersDropDown();
            PopulateEnumsDropDowns();
            var model = new Tache { DateCreation = DateTime.Now };
            return View("~/Views/Taches/Create.cshtml", model);
        }

        // POST: /Taches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tache tache)
        {
            PopulateProjectsDropDown(tache.ProjectID);
            PopulateSprintsDropDown(tache.SprintID);
            PopulateUsersDropDown(tache.AssigneeID ?? 0);
            PopulateEnumsDropDowns();

            if (!ModelState.IsValid)
                return View("~/Views/Taches/Create.cshtml", tache);

            tache.DateCreation = DateTime.Now;

            try
            {
                _context.Taches.Add(tache);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la tâche");
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la sauvegarde.");
                return View("~/Views/Taches/Create.cshtml", tache);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Taches/Edit/5
        public IActionResult Edit(int id)
        {
            var tache = _context.Taches.Find(id);
            if (tache == null) return NotFound();

            PopulateProjectsDropDown(tache.ProjectID);
            PopulateSprintsDropDown(tache.SprintID);
            PopulateUsersDropDown(tache.AssigneeID ?? 0);
            PopulateEnumsDropDowns(tache.Statut, tache.Type, tache.Priorite);

            return View("~/Views/Taches/Edit.cshtml", tache);
        }

        // POST: /Taches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Tache updated)
        {
            if (id != updated.TacheID) return BadRequest();

            PopulateProjectsDropDown(updated.ProjectID);
            PopulateSprintsDropDown(updated.SprintID);
            PopulateUsersDropDown(updated.AssigneeID ?? 0);
            PopulateEnumsDropDowns(updated.Statut, updated.Type, updated.Priorite);

            if (!ModelState.IsValid)
                return View("~/Views/Taches/Edit.cshtml", updated);

            var existing = _context.Taches.FirstOrDefault(t => t.TacheID == id);
            if (existing == null) return NotFound();

            existing.Titre = updated.Titre;
            existing.Description = updated.Description;
            existing.Type = updated.Type;
            existing.Priorite = updated.Priorite;
            existing.Statut = updated.Statut;
            existing.EstimationDur = updated.EstimationDur;
            existing.TempsRestant = updated.TempsRestant;
            existing.DateMiseAJour = DateTime.Now;
            existing.DateResolution = updated.DateResolution;
            existing.ProjectID = updated.ProjectID;
            existing.SprintID = updated.SprintID;
            existing.AssigneeID = updated.AssigneeID;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la tâche ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour.");
                return View("~/Views/Taches/Edit.cshtml", updated);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Taches/Delete/5
        public IActionResult Delete(int id)
        {
            var tache = _context.Taches
                .Include(t => t.Projet)
                .FirstOrDefault(t => t.TacheID == id);
            if (tache == null) return NotFound();
            return View("~/Views/Taches/Delete.cshtml", tache);
        }

        // POST: /Taches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var tache = _context.Taches.Find(id);
            if (tache == null) return NotFound();

            try
            {
                _context.Taches.Remove(tache);
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la tâche ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Impossible de supprimer la tâche (contraintes DB).");
                return View("~/Views/Taches/Delete.cshtml", tache);
            }

            return RedirectToAction(nameof(Index));
        }

        // -------- Helpers pour dropdowns ----------
        private void PopulateProjectsDropDown(object selected = null)
        {
            ViewBag.Projets = _context.Projets
                .AsNoTracking()
                .OrderBy(p => p.Nom)
                .Select(p => new SelectListItem { Value = p.ProjectID.ToString(), Text = p.Nom })
                .ToList();
        }

        private void PopulateSprintsDropDown(object selected = null)
        {
            ViewBag.Sprints = _context.Sprints
                .AsNoTracking()
                .OrderBy(s => s.DateDebut)
                .Select(s => new SelectListItem { Value = s.SprintID.ToString(), Text = s.Nom })
                .ToList();
        }

        private void PopulateUsersDropDown(object selected = null)
        {
            // selected may be int or string; try to normalize to int for selection comparison
            int? selectedId = null;
            if (selected is int si) selectedId = si;
            else if (selected != null && int.TryParse(selected.ToString(), out var parsed)) selectedId = parsed;

            ViewBag.Utilisateurs = _context.Utilisateurs
                .AsNoTracking()
                .OrderBy(u => u.Nom)
                .Select(u => new SelectListItem
                {
                    Value = u.UserID.ToString(), // corrected: Utilisateur.UserID is the actual PK
                    Text = string.IsNullOrWhiteSpace(u.Nom) ? u.Email : u.Nom,
                    Selected = selectedId.HasValue && selectedId.Value == u.UserID
                })
                .ToList();
        }

        private void PopulateEnumsDropDowns(StatutTache? selectedStatut = null, TypeTache? selectedType = null, PrioriteTache? selectedPriorite = null)
        {
            ViewBag.Statuts = Enum.GetValues(typeof(StatutTache))
                                 .Cast<StatutTache>()
                                 .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString(), Selected = (selectedStatut.HasValue && selectedStatut.Value == s) })
                                 .ToList();

            ViewBag.Types = Enum.GetValues(typeof(TypeTache))
                                .Cast<TypeTache>()
                                .Select(t => new SelectListItem { Value = t.ToString(), Text = t.ToString(), Selected = (selectedType.HasValue && selectedType.Value == t) })
                                .ToList();

            ViewBag.Priorites = Enum.GetValues(typeof(PrioriteTache))
                                   .Cast<PrioriteTache>()
                                   .Select(p => new SelectListItem { Value = p.ToString(), Text = p.ToString(), Selected = (selectedPriorite.HasValue && selectedPriorite.Value == p) })
                                   .ToList();
        }
    }
}