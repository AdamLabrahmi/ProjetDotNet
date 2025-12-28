using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models.ViewModels;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel();

            var current = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (current == 0)
            {
                model = new DashboardViewModel
                {
                    TotalUsers = 0,
                    TotalProjects = 0,
                    TotalTasks = 0,
                    TotalTeams = 0,
                    MyProjects = 0,
                    MyTasks = 0,
                    MyTeams = 0,
                    IsSiteAdmin = false
                };
                ViewBag.TasksByStatusJson = "[]";
                ViewBag.TasksByPriorityJson = "[]";
                ViewBag.Debug = "{}";
                return View("~/Views/Dashboard/Index.cshtml", model);
            }

            var isSiteAdmin = AuthorizationHelper.IsSiteAdmin(_context, current);

            if (isSiteAdmin)
            {
                // statistiques globales
                model = new DashboardViewModel
                {
                    IsSiteAdmin = true,
                    TotalUsers = _context.Utilisateurs.AsNoTracking().Count(),
                    TotalProjects = _context.Projets.AsNoTracking().Count(),
                    TotalTasks = _context.Taches.AsNoTracking().Count(),
                    TotalTeams = _context.Equipes.AsNoTracking().Count(),
                    MyProjects = 0,
                    MyTasks = 0,
                    MyTeams = 0
                };

                // tâches visibles = toutes
                var allTasks = _context.Taches.AsNoTracking().AsQueryable();

                var tasksByStatus = allTasks
                    .GroupBy(t => t.Statut)
                    .Select(g => new { statut = g.Key.ToString(), count = g.Count() })
                    .ToList();

                var tasksByPriority = allTasks
                    .GroupBy(t => t.Priorite)
                    .Select(g => new { priority = g.Key.ToString(), count = g.Count() })
                    .ToList();

                ViewBag.TasksByStatusJson = JsonSerializer.Serialize(tasksByStatus);
                ViewBag.TasksByPriorityJson = JsonSerializer.Serialize(tasksByPriority);
                ViewBag.Debug = JsonSerializer.Serialize(new { scope = "admin" });

                return View("~/Views/Dashboard/Index.cshtml", model);
            }

            // ---------- Utilisateur non-admin : calculs explicites ----------
            // projets dont l'utilisateur est membre (MembreProjet)
            var myProjectIds = _context.MembreProjets
                .AsNoTracking()
                .Where(mp => mp.UserID == current)
                .Select(mp => mp.ProjectID)
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            // équipes dont l'utilisateur est membre (MembreEquipe)
            var myTeamIds = _context.MembreEquipes
                .AsNoTracking()
                .Where(me => me.UserID == current)
                .Select(me => me.TeamID)
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            // organisations reliées aux équipes de l'utilisateur (optionnel pour inclusion des projets d'orga)
            var orgIdsFromTeams = _context.Equipes
                .AsNoTracking()
                .Where(e => myTeamIds.Contains(e.TeamID))
                .Select(e => e.OrgID)
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            // projets qui appartiennent aux organisations des équipes
            var projectsFromOrgs = _context.Projets
                .AsNoTracking()
                .Where(p => orgIdsFromTeams.Contains(p.OrgID))
                .Select(p => p.ProjectID)
                .Distinct()
                .ToList();

            // union propre des projets visibles pour l'utilisateur
            var effectiveProjectIds = myProjectIds.Union(projectsFromOrgs).Distinct().ToList();

            // construire le modèle utilisateur
            model = new DashboardViewModel
            {
                IsSiteAdmin = false,
                TotalUsers = _context.Utilisateurs.AsNoTracking().Count(),
                TotalProjects = _context.Projets.AsNoTracking().Count(),
                TotalTasks = _context.Taches.AsNoTracking().Count(),
                TotalTeams = _context.Equipes.AsNoTracking().Count(),

                MyProjects = effectiveProjectIds.Count,
                MyTeams = myTeamIds.Count,
                MyTasks = _context.Taches.AsNoTracking().Count(t =>
                    (t.AssigneeID.HasValue && t.AssigneeID.Value == current)
                    || (t.ProjectID.HasValue && effectiveProjectIds.Contains(t.ProjectID.Value)))
            };

            // --- données graphiques restreintes à la portée ---
            var tasksQuery = _context.Taches.AsNoTracking()
                .Where(t => (t.AssigneeID.HasValue && t.AssigneeID.Value == current)
                            || (t.ProjectID.HasValue && effectiveProjectIds.Contains(t.ProjectID.Value)));

            var tasksByStatusFiltered = tasksQuery
                .GroupBy(t => t.Statut)
                .Select(g => new { statut = g.Key.ToString(), count = g.Count() })
                .ToList();

            var tasksByPriorityFiltered = tasksQuery
                .GroupBy(t => t.Priorite)
                .Select(g => new { priority = g.Key.ToString(), count = g.Count() })
                .ToList();

            ViewBag.TasksByStatusJson = JsonSerializer.Serialize(tasksByStatusFiltered);
            ViewBag.TasksByPriorityJson = JsonSerializer.Serialize(tasksByPriorityFiltered);

            // DEBUG pour vérifier les IDs calculés (supprimer en production)
            ViewBag.Debug = JsonSerializer.Serialize(new
            {
                current,
                myProjectIds,
                myTeamIds,
                orgIdsFromTeams,
                projectsFromOrgs,
                effectiveProjectIds,
                myProjectsCount = effectiveProjectIds.Count,
                myTeamsCount = myTeamIds.Count
            });

            return View("~/Views/Dashboard/Index.cshtml", model);
        }
    }
}