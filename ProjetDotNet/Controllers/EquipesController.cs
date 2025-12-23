using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetDotNet.Data;
using ProjetDotNet.Models;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class EquipesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EquipesController> _logger;

        public EquipesController(AppDbContext context, ILogger<EquipesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Equipes or /Equipes?orgId=1
        public IActionResult Index(int? orgId)
        {
            ViewBag.OrgID = orgId;

            var totalCount = _context.Equipes.Count();
            _logger.LogInformation("Equipes total en base: {Count}", totalCount);

            IQueryable<Equipe> query = _context.Equipes.AsNoTracking();

            if (orgId.HasValue)
            {
                query = query.Where(e => e.OrgID == orgId.Value);
                _logger.LogInformation("Filtre OrgID={OrgId}", orgId.Value);
            }

            var listes = query
                .OrderBy(e => e.Nom)
                .ToList();

            _logger.LogInformation("Equipes retournées: {Count}", listes.Count);
            return View("~/Views/Equipes/Index.cshtml", listes);
        }

        // GET: Create - affiche la liste des organisations pour choix manuel
        public IActionResult Create(int? orgId)
        {
            ViewBag.Organisations = _context.Organisations
                .AsNoTracking()
                .OrderBy(o => o.Nom)
                .Select(o => new SelectListItem
                {
                    Value = o.OrgID.ToString(),
                    Text = o.Nom
                })
                .ToList();

            var equipe = new Equipe();

            if (orgId.HasValue)
                equipe.OrgID = orgId.Value;

            return View(equipe);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Equipe equipe)
        {
            ViewBag.Organisations = _context.Organisations
                .AsNoTracking()
                .OrderBy(o => o.Nom)
                .Select(o => new SelectListItem
                {
                    Value = o.OrgID.ToString(),
                    Text = o.Nom
                })
                .ToList();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "ModelState invalide pour Create Equipe: {Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                );
                return View(equipe);
            }

            if (!_context.Organisations.Any(o => o.OrgID == equipe.OrgID))
            {
                ModelState.AddModelError(nameof(equipe.OrgID), "Organisation invalide.");
                return View(equipe);
            }

            try
            {
                equipe.DateCreation = DateTime.Now;
                _context.Equipes.Add(equipe);
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur création équipe");
                ModelState.AddModelError("", "Erreur serveur lors de l'enregistrement.");
                return View(equipe);
            }

            return RedirectToAction("Index", new { orgId = equipe.OrgID });
        }


        public IActionResult Edit(int id)
        {
            var equipe = _context.Equipes.Find(id);
            if (equipe == null) return NotFound();

            // fournir la liste des organisations si vous voulez permettre le changement d'Org
            ViewBag.Organisations = _context.Organisations
                .AsNoTracking()
                .OrderBy(o => o.Nom)
                .Select(o => new SelectListItem { Value = o.OrgID.ToString(), Text = o.Nom })
                .ToList();

            return View(equipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Equipe equipe)
        {
            ViewBag.Organisations = _context.Organisations
                .AsNoTracking()
                .OrderBy(o => o.Nom)
                .Select(o => new SelectListItem { Value = o.OrgID.ToString(), Text = o.Nom })
                .ToList();

            if (!ModelState.IsValid)
                return View(equipe);

            var existing = _context.Equipes.Find(equipe.TeamID);
            if (existing == null) return NotFound();

            // Mettre à jour uniquement les champs éditables
            existing.Nom = equipe.Nom;
            existing.Description = equipe.Description;
            existing.OrgID = equipe.OrgID;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'équipe ID {Id}", equipe.TeamID);
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la mise à jour. Voir logs.");
                return View(equipe);
            }

            return RedirectToAction("Index", new { orgId = existing.OrgID });
        }

        public IActionResult Delete(int id)
        {
            var equipe = _context.Equipes.Find(id);
            if (equipe == null) return NotFound();

            return View(equipe);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var equipe = _context.Equipes.Find(id);
            if (equipe == null) return NotFound();

            int orgId = equipe.OrgID;

            _context.Equipes.Remove(equipe);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
