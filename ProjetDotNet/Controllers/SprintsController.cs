using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetDotNet.Data;
using ProjetDotNet.Models;

namespace ProjetDotNet.Controllers
{
    public class SprintsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SprintsController> _logger;

        public SprintsController(AppDbContext context, ILogger<SprintsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Sprints
        public IActionResult Index()
        {
            var sprints = _context.Sprints
                .Include(s => s.Projet)
                .OrderByDescending(s => s.DateCreation)
                .ToList();

            return View("~/Views/Sprints/Index.cshtml", sprints);
        }


        // GET: /Sprints/Details/5
        public IActionResult Details(int id)
        {
            var sprint = _context.Sprints
                .Include(s => s.Projet)
                .FirstOrDefault(s => s.SprintID == id);

            if (sprint == null)
            {
                _logger.LogWarning("Détails demandés pour un sprint introuvable (ID = {Id})", id);
                TempData["AlertDanger"] = $"Sprint introuvable (ID = {id}).";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Sprints/Details.cshtml", sprint);
        }


        // GET: /Sprints/Create
        public IActionResult Create()
        {
            PopulateProjectsDropDown();
            return View("~/Views/Sprints/Create.cshtml", new Sprint
            {
                DateDebut = DateTime.Today,
                DateFin = DateTime.Today.AddDays(14)
            });
        }

        // POST: /Sprints/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Sprint sprint)
        {
            PopulateProjectsDropDown(sprint.ProjectID);

            if (!ModelState.IsValid)
                return View("~/Views/Sprints/Create.cshtml", sprint);

            try
            {
                sprint.DateCreation = DateTime.Now;
                _context.Sprints.Add(sprint);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du sprint");
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la sauvegarde.");
                return View("~/Views/Sprints/Create.cshtml", sprint);
            }

            return RedirectToAction(nameof(Index));
        }



        // GET: /Sprints/Edit/5
        public IActionResult Edit(int id)
        {
            var sprint = _context.Sprints.Find(id);
            if (sprint == null)
            {
                _logger.LogWarning("Tentative de modification d'un sprint introuvable (ID = {Id})", id);
                TempData["AlertDanger"] = $"Sprint introuvable (ID = {id}).";
                return RedirectToAction(nameof(Index));
            }

            PopulateProjectsDropDown(sprint.ProjectID);
            return View("~/Views/Sprints/Edit.cshtml", sprint);
        }

        // POST: /Sprints/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Sprint updated)
        {
            if (id != updated.SprintID)
                return BadRequest();

            PopulateProjectsDropDown(updated.ProjectID);

            if (!ModelState.IsValid)
                return View("~/Views/Sprints/Edit.cshtml", updated);

            var existing = _context.Sprints.FirstOrDefault(s => s.SprintID == id);
            if (existing == null)
            {
                _logger.LogWarning("Tentative de mise à jour d'un sprint introuvable (ID = {Id})", id);
                TempData["AlertDanger"] = $"Sprint introuvable (ID = {id}).";
                return RedirectToAction(nameof(Index));
            }

            existing.Nom = updated.Nom;
            existing.Objectif = updated.Objectif;
            existing.DateDebut = updated.DateDebut;
            existing.DateFin = updated.DateFin;
            existing.Statut = updated.Statut;
            existing.ProjectID = updated.ProjectID;

            try
            {
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Sprint mis à jour.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du sprint ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour.");
                return View("~/Views/Sprints/Edit.cshtml", updated);
            }

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Delete(int id)
        {
            var sprint = _context.Sprints
                .Include(s => s.Projet)
                .FirstOrDefault(s => s.SprintID == id);

            if (sprint == null)
            {
                _logger.LogWarning("Tentative de suppression d'un sprint introuvable (ID = {Id})", id);
                TempData["AlertDanger"] = $"Sprint introuvable (ID = {id}).";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Sprints/Delete.cshtml", sprint);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var sprint = _context.Sprints.Find(id);
            if (sprint == null)
            {
                TempData["AlertDanger"] = $"Sprint introuvable (ID = {id}).";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Sprints.Remove(sprint);
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Sprint supprimé.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du sprint ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Impossible de supprimer le sprint (contraintes DB).");
                return View("~/Views/Sprints/Delete.cshtml", sprint);
            }

            return RedirectToAction(nameof(Index));
        }


        private void PopulateProjectsDropDown(object selected = null)
        {
            ViewBag.Projets = _context.Projets
                .AsNoTracking()
                .OrderBy(p => p.Nom)
                .Select(p => new SelectListItem
                {
                    Value = p.ProjectID.ToString(),
                    Text = p.Nom,
                    Selected = selected != null && selected.ToString() == p.ProjectID.ToString()
                })
                .ToList();
        }
    }
}
