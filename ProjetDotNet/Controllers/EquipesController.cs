using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;
using System;
using System.Security.Claims;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class EquipesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EquipesController> _logger;
        //private readonly IEmailSender _emailSender;

        public EquipesController(AppDbContext context, ILogger<EquipesController> logger)
        {
            _context = context;
            _logger = logger;
            //_emailSender = emailSender;
        }

        // GET: /Equipes or /Equipes?orgId=1
        public IActionResult Index(int? orgId)
        {
            ViewBag.OrgID = orgId;

            var totalCount = _context.Equipes.Count();
            _logger.LogInformation("Equipes total en base: {Count}", totalCount);

            // récupérer l'utilisateur courant
            var current = AuthorizationHelper.GetCurrentUserId(User, _context);
            var isSiteAdmin = AuthorizationHelper.IsSiteAdmin(_context, current);

            // Construire la requête initiale (inclure membres/utilisateurs pour la vue)
            var query = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Membres)
                    .ThenInclude(m => m.Utilisateur)
                .AsQueryable();

            if (orgId.HasValue)
            {
                query = query.Where(e => e.OrgID == orgId.Value);
                _logger.LogInformation("Filtre OrgID={OrgId}", orgId.Value);
            }

            if (!isSiteAdmin)
            {
                // restreindre aux équipes dont l'utilisateur est membre
                query = query.Where(e => e.Membres.Any(me => me.UserID == current));
            }

            var listes = query
                .OrderBy(e => e.Nom)
                .ToList();

            // message si aucune équipe visible
            if (!isSiteAdmin && (listes == null || listes.Count == 0))
            {
                ViewBag.InfoMessage = "Vous n'êtes membre d'aucune équipe pour le moment.";
            }

            _logger.LogInformation("Equipes retournées: {Count}", listes.Count);
            return View("~/Views/Equipes/Index.cshtml", listes);
        }

        // GET: /Equipes/Details/5
        //public IActionResult Details(int id)
        //{
        //    var equipe = _context.Equipes
        //        .Include(e => e.Membres)
        //            .ThenInclude(m => m.Utilisateur)
        //        .FirstOrDefault(e => e.TeamID == id);

        //    if (equipe == null)
        //        return NotFound();

        //    return View(equipe);
        //}


      
        public IActionResult Details(int id)
        {
            var equipe = _context.Equipes
                .AsNoTracking()
                .Include(e => e.Membres!)
                    .ThenInclude(me => me.Utilisateur)
                .Include(e => e.Organisation)
                .FirstOrDefault(e => e.TeamID == id);

            if (equipe == null) return NotFound();

            return View("~/Views/Equipes/Details.cshtml", equipe);
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
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var equipe = _context.Equipes
                    .Include(e => e.Membres)   // MembreEquipe
                    .FirstOrDefault(e => e.TeamID == id);

                if (equipe == null) return NotFound();

                // 1) Supprimer explicitement les membres de l'équipe
                if (equipe.Membres != null && equipe.Membres.Any())
                {
                    _context.MembreEquipes.RemoveRange(equipe.Membres);
                }

                // 2) Si vous avez des entités liées (projets liés à l'équipe, etc.), gérez-les ici.
                //    Exemple : désassocier les projets -> _context.Projets.Where(p => p.TeamID == id)...
                //    (adapter selon votre modèle si nécessaire)

                // 3) Supprimer l'équipe
                _context.Equipes.Remove(equipe);

                _context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ModelState.AddModelError(string.Empty, "Impossible de supprimer l'équipe : " + ex.Message);
                var teamView = _context.Equipes.Find(id);
                return View("~/Views/Equipes/Delete.cshtml", teamView);
            }

            // Rediriger vers la liste ou la page organisation
            return RedirectToAction(nameof(Index));
        }

        
    }
}
