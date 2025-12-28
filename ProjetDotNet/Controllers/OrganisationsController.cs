using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class OrganisationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrganisationsController> _logger;

        public OrganisationsController(AppDbContext context, ILogger<OrganisationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var orgs = _context.Organisations
                .Include(o => o.Admin)
                .Include(o => o.Projets)
                .Include(o => o.Equipes)
                .ToList();
            return View("~/Views/Organisations/Index.cshtml", orgs);
        }

        public IActionResult Details(int id)
        {
            var org = _context.Organisations
                .AsNoTracking()
                .Include(o => o.Admin)
                    .ThenInclude(a => a.Utilisateur)
                .Include(o => o.Equipes)
                    .ThenInclude(e => e.Membres)
                        .ThenInclude(me => me.Utilisateur)
                .Include(o => o.Projets)
                    .ThenInclude(p => p.Membres)
                        .ThenInclude(mp => mp.Utilisateur)
                .FirstOrDefault(o => o.OrgID == id);

            if (org == null)
            {
                _logger.LogWarning("Organisation introuvable (ID = {Id})", id);
                return NotFound();
            }

            var currentUserId = AuthorizationHelper.GetCurrentUserId(User, _context);
            ViewBag.CanManageOrg = AuthorizationHelper.IsSiteAdmin(_context, currentUserId) || org.AdminID == currentUserId;

            return View("~/Views/Organisations/Details.cshtml", org);
        }
        public IActionResult Create()
        {
            return View("~/Views/Organisations/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Organisations org)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalide lors de la création d'une organisation: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View("~/Views/Organisations/Create.cshtml", org);
            }

            int userId;
            try
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible de lire l'identifiant utilisateur.");
                ModelState.AddModelError(string.Empty, "Erreur d'authentification utilisateur.");
                return View("~/Views/Organisations/Create.cshtml", org);
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var admin = _context.Admins
                    .Include(a => a.Organisations)
                    .FirstOrDefault(a => a.UserID == userId);

                if (admin == null)
                {
                    admin = new Admin
                    {
                        UserID = userId,
                        Organisations = new List<Organisations>()
                    };
                    _context.Admins.Add(admin);
                    _context.SaveChanges();
                }

                org.AdminID = admin.UserID;
                org.Admin = admin;
                org.DateCreation = DateTime.Now;
                org.Equipes = new List<Equipe>();
                org.Projets = new List<Projet>();

                _context.Organisations.Add(org);
                _context.SaveChanges();

                var projetInitial = new Projet
                {
                    Nom = $"{org.Nom} - Projet initial",
                    CleProjet = GenerateProjectKey(org.Nom),
                    Description = $"Projet créé automatiquement pour l'organisation {org.Nom}",
                    DateDebut = DateTime.Now,
                    DateFin = DateTime.Now.AddMonths(3),
                    Statut = StatutProjet.EN_ATTENTE,
                    DateCreation = DateTime.Now,
                    OrgID = org.OrgID,
                    Organisation = org
                };
                _context.Projets.Add(projetInitial);
                _context.SaveChanges();

                var equipeInitial = new Equipe
                {
                    Nom = $"{org.Nom} - Équipe initiale",
                    Description = "Équipe créée automatiquement",
                    DateCreation = DateTime.Now,
                    OrgID = org.OrgID,
                    Organisation = org
                };
                _context.Equipes.Add(equipeInitial);
                _context.SaveChanges();

                if (admin.Organisations == null)
                    admin.Organisations = new List<Organisations>();
                admin.Organisations.Add(org);
                _context.Admins.Update(admin);
                _context.SaveChanges();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Erreur lors de la création complète de l'organisation (Admin/Projet/Equipe).");
                ModelState.AddModelError(string.Empty, "Erreur serveur lors de la création : " + ex.Message);
                return View("~/Views/Organisations/Create.cshtml", org);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(int id)
        {
            var org = _context.Organisations.Find(id);
            if (org == null) return NotFound();

            return View("~/Views/Organisations/Edit.cshtml", org);
        }

        // =========================
        // EDIT (POST)  -- préserve AdminID et met à jour les noms d'équipes
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Organisations updatedOrg)
        {
            if (id != updatedOrg.OrgID)
                return BadRequest();

            var org = _context.Organisations.FirstOrDefault(o => o.OrgID == id);
            if (org == null) return NotFound();

            // Validez uniquement les champs souhaités
            if (!TryValidateModel(new { updatedOrg.Nom, updatedOrg.Description }, nameof(updatedOrg)))
            {
                return View("~/Views/Organisations/Edit.cshtml", updatedOrg);
            }

            // Conserver l'ancien nom pour mise à jour des équipes
            var oldName = org.Nom ?? string.Empty;
            var newName = updatedOrg.Nom ?? string.Empty;

            // Mettre à jour uniquement les propriétés éditables (ne pas écraser AdminID)
            org.Nom = newName;
            org.Description = updatedOrg.Description;
            // autres champs éditables si nécessaire...

            // Mettre à jour les équipes liées dont le nom commence par "{oldName} -"
            try
            {
                var equipes = _context.Equipes.Where(e => e.OrgID == id).ToList();
                if (!string.IsNullOrWhiteSpace(oldName) && !string.Equals(oldName, newName, StringComparison.Ordinal))
                {
                    foreach (var equipe in equipes)
                    {
                        if (!string.IsNullOrEmpty(equipe.Nom) && equipe.Nom.StartsWith(oldName + " - ", StringComparison.Ordinal))
                        {
                            // Remplace uniquement le préfixe ancien -> nouveau
                            equipe.Nom = newName + equipe.Nom.Substring(oldName.Length);
                        }
                    }
                    _context.SaveChanges();
                }
                else
                {
                    // si oldName vide ou inchangé, rien à modifier sur les équipes
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des équipes liées à l'organisation ID {OrgID}", id);
                ModelState.AddModelError(string.Empty, "Erreur lors de la mise à jour des équipes : " + ex.Message);
                return View("~/Views/Organisations/Edit.cshtml", updatedOrg);
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE (GET)
        // =========================
        public IActionResult Delete(int id)
        {
            var org = _context.Organisations.Find(id);
            if (org == null) return NotFound();

            return View("~/Views/Organisations/Delete.cshtml", org);
        }

        // =========================
        // DELETE (POST)
        // =========================
        // ... (conserver le reste du fichier inchangé) ...

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var org = _context.Organisations
                    .Include(o => o.Projets)
                    .Include(o => o.Equipes)
                    .FirstOrDefault(o => o.OrgID == id);

                if (org == null) return NotFound();

                if (org.Projets != null && org.Projets.Any())
                    _context.Projets.RemoveRange(org.Projets);

                if (org.Equipes != null && org.Equipes.Any())
                    _context.Equipes.RemoveRange(org.Equipes);

                _context.Organisations.Remove(org);

                _context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Erreur lors de la suppression de l'organisation ID {Id}", id);
                ModelState.AddModelError(string.Empty, "Impossible de supprimer l'organisation : " + ex.Message);
                var orgView = _context.Organisations.Find(id);
                return View("~/Views/Organisations/Delete.cshtml", orgView);
            }

            return RedirectToAction(nameof(Index));
        }

        // ... (conserver le reste du fichier inchangé) ...

        private static string GenerateProjectKey(string orgName)
        {
            if (string.IsNullOrWhiteSpace(orgName))
                orgName = "PRJ";
            var prefix = new string(orgName.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            return $"{prefix}-{suffix}";
        }
    }
}