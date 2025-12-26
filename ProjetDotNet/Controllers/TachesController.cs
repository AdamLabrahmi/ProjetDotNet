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
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User, _context);
            var isSiteAdmin = AuthorizationHelper.IsSiteAdmin(_context, currentUserId);

            var query = _context.Taches
                .AsNoTracking()
                .Include(t => t.Projet)
                .Include(t => t.Sprint)
                .Include(t => t.Assignee)
                .Include(t => t.Createur)
                .AsQueryable();

            if (!isSiteAdmin)
            {
                // uniquement les tâches des projets où l'utilisateur est membre
                query = query.Where(t => t.Projet.Membres.Any(mp => mp.UserID == currentUserId));
            }

            var taches = query
                .OrderByDescending(t => t.DateCreation)
                .ToList();

            return View("~/Views/Taches/Index.cshtml", taches);
        }

        // GET: /Taches/Details/5
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

        // GET: /Taches/Create
        public IActionResult Create()
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (currentUserId == 0) return Challenge();

            // Construire la liste des projets que l'utilisateur peut utiliser pour créer une tâche
            var projetsQuery = _context.Projets.AsNoTracking().AsQueryable();

            if (!AuthorizationHelper.IsSiteAdmin(_context, currentUserId))
            {
                // projets où l'utilisateur a rôle projet autorisé OR est dans une équipe autorisée
                var projectIdsFromProjectRole = _context.MembreProjets
                    .Where(mp => mp.UserID == currentUserId &&
                                 (mp.Role == RoleProjet.ProductOwner || mp.Role == RoleProjet.ScrumMaster || mp.Role == RoleProjet.Testeur))
                    .Select(mp => mp.ProjectID)
                    .Distinct();

                var teamIdsUser = _context.MembreEquipes
                    .Where(me => me.UserID == currentUserId)
                    .Select(me => me.TeamID)
                    .Distinct()
                    .ToList();

                var orgIdsFromTeams = _context.Equipes
                    .Where(e => teamIdsUser.Contains(e.TeamID))
                    .Select(e => e.OrgID)
                    .Distinct();

                var projectIdsFromOrga = _context.Projets
                    .Where(p => orgIdsFromTeams.Contains(p.OrgID))
                    .Select(p => p.ProjectID);

                var allowedProjectIds = projectIdsFromProjectRole
                    .Union(projectIdsFromOrga)
                    .Distinct();

                projetsQuery = projetsQuery.Where(p => allowedProjectIds.Contains(p.ProjectID));
            }

            ViewBag.Projets = projetsQuery
                .OrderBy(p => p.Nom)
                .Select(p => new SelectListItem { Value = p.ProjectID.ToString(), Text = p.Nom })
                .ToList();

            // Préparer autres dropdowns (vide pour sprint/assignee ; chargés via JS)
            PopulateSprintsDropDown();
            PopulateUsersDropDown();
            PopulateEnumsDropDowns();

            // Vérifier si des projets existent pour l'utilisateur
            if (!((System.Collections.Generic.List<SelectListItem>)ViewBag.Projets).Any())
            {
                ModelState.AddModelError(string.Empty, "Aucun projet disponible pour créer une tâche.");
                return View("~/Views/Taches/Create.cshtml", new Tache());
            }

            var model = new Tache { DateCreation = DateTime.Now, CreateurID = currentUserId };
            return View("~/Views/Taches/Create.cshtml", model);
        }

        // POST: /Taches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tache tache)
        {
            int currentUserId = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (currentUserId == 0) return Challenge();

            // 1) Assurer que CreateurID est renseigné AVANT la validation
            if (tache.CreateurID == 0)
            {
                tache.CreateurID = currentUserId;
            }

            // Si ModelState contient une ancienne erreur sur CreateurID, la retirer pour re-validation
            if (ModelState.ContainsKey(nameof(Tache.CreateurID)))
            {
                ModelState.Remove(nameof(Tache.CreateurID));
            }

            // 2) Repeupler les dropdowns pour l'affichage en cas d'erreur
            PopulateProjectsDropDown(tache.ProjectID);
            PopulateSprintsDropDown(tache.SprintID);
            PopulateUsersDropDown(tache.AssigneeID ?? 0);
            PopulateEnumsDropDowns();

            // 3) Vérification explicite : ProjectID est nullable (int?) -> utiliser HasValue
            if (!tache.ProjectID.HasValue || tache.ProjectID.Value == 0)
            {
                ModelState.AddModelError("ProjectID", "Veuillez sélectionner un projet.");
            }

            // ----- AUTORISATION: seul SiteAdmin ou rôles permis peuvent créer une tâche pour ce projet -----
            if (!tache.ProjectID.HasValue || !AuthorizationHelper.CanCreateTache(_context, currentUserId, tache.ProjectID.Value))
            {
                ModelState.AddModelError(string.Empty, "Vous n'êtes pas autorisé à créer une tâche pour ce projet.");
            }

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    var field = entry.Key;
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogError("❌ Champ: {Field} | Erreur: {Error}", field, error.ErrorMessage);
                    }
                }

                return View("~/Views/Taches/Create.cshtml", tache);
            }

            // 5) Persist
            tache.DateCreation = DateTime.Now;

            _context.Taches.Add(tache);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetSprintsByProject(int projectId)
        {
            var sprints = _context.Sprints
                .Where(s => s.ProjectID == projectId)
                .OrderBy(s => s.DateDebut)
                .Select(s => new
                {
                    s.SprintID,
                    s.Nom
                })
                .ToList();

            return Json(sprints);
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
            // updated.ProjectID est int? maintenant — préserver si null
            existing.ProjectID = updated.ProjectID ?? existing.ProjectID;
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
            int? selectedId = null;
            if (selected is int si) selectedId = si;
            else if (selected != null && int.TryParse(selected.ToString(), out var parsed)) selectedId = parsed;

            ViewBag.Projets = _context.Projets
                .AsNoTracking()
                .OrderBy(p => p.Nom)
                .Select(p => new SelectListItem
                {
                    Value = p.ProjectID.ToString(),
                    Text = p.Nom,
                    Selected = selectedId.HasValue && selectedId.Value == p.ProjectID
                })
                .ToList();
        }

        private void PopulateSprintsDropDown(object selected = null)
        {
            int? selectedId = null;
            if (selected is int si) selectedId = si;
            else if (selected != null && int.TryParse(selected.ToString(), out var parsed)) selectedId = parsed;

            ViewBag.Sprints = _context.Sprints
                .AsNoTracking()
                .OrderBy(s => s.DateDebut)
                .Select(s => new SelectListItem
                {
                    Value = s.SprintID.ToString(),
                    Text = s.Nom,
                    Selected = selectedId.HasValue && selectedId.Value == s.SprintID
                })
                .ToList();
        }

        // Corrigé : accepte un paramètre sélectionné
        private void PopulateUsersDropDown(object selected = null)
        {
            int? selectedId = null;
            if (selected is int si) selectedId = si;
            else if (selected != null && int.TryParse(selected.ToString(), out var parsed)) selectedId = parsed;

            ViewBag.Utilisateurs = _context.Utilisateurs
                .AsNoTracking()
                .OrderBy(u => u.Nom)
                .Select(u => new SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = string.IsNullOrWhiteSpace(u.Nom) ? u.Email : u.Nom,
                    Selected = selectedId.HasValue && selectedId.Value == u.UserID
                })
                .ToList();
        }

        [HttpGet]
        public IActionResult GetMembresByEquipe(int equipeId, bool excludeLeads = false)
        {
            var query = _context.MembreEquipes
                .Where(me => me.TeamID == equipeId);

            if (excludeLeads)
            {
                // exclure ScrumMaster et ProductOwner
                query = query.Where(me => me.Role != RoleEquipe.ScrumMaster && me.Role != RoleEquipe.ProductOwner);
            }

            var membres = query
                .Select(me => new
                {
                    UserID = me.Utilisateur.UserID,
                    Nom = string.IsNullOrEmpty(me.Utilisateur.Nom) ? me.Utilisateur.Email : me.Utilisateur.Nom
                })
                .ToList();

            return Json(membres);
        }


        [HttpGet]
        public IActionResult GetEquipesByProjet(int projectId)
        {
            // Récupérer l'OrgID du projet, puis les équipes liées via OrgID (propriété 'OrgID' sur Equipe)
            var orgId = _context.Projets
                .Where(p => p.ProjectID == projectId)
                .Select(p => p.OrgID)
                .FirstOrDefault();

            var equipes = _context.Equipes
                .Where(e => e.OrgID == orgId)
                .Select(e => new
                {
                    e.TeamID,
                    e.Nom
                })
                .ToList();

            return Json(equipes);
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

        // -------- Helper pour récupérer l'ID utilisateur connecté ----------
        private int GetCurrentUserId()
        {
            // 1) Essayer NameIdentifier (ID numérique direct)
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var idFromClaim))
                return idFromClaim;

            // 2) Fallback sur email
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(email))
            {
                var user = _context.Utilisateurs.AsNoTracking().FirstOrDefault(u => u.Email == email);
                if (user != null)
                    return user.UserID;
            }

            // 3) Si rien, log et retourne 0
            _logger.LogWarning("Impossible de récupérer l'ID utilisateur. Claims: {@Claims}", User.Claims.Select(c => new { c.Type, c.Value }));
            return 0;
        }
    }
}