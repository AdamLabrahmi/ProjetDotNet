using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetDotNet.Data;
using ProjetDotNet.Helpers;
using ProjetDotNet.Models;
using ProjetDotNet.Models.ViewModels;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(AppDbContext context, IWebHostEnvironment env, ILogger<ProfileController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: /Profile
        public IActionResult Index()
        {
            var current = AuthorizationHelper.GetCurrentUserId(User, _context);
            if (current == 0) return Challenge();

            // IDs d'équipes et de projets où l'utilisateur est membre
            var myTeamIds = _context.MembreEquipes
                .AsNoTracking()
                .Where(me => me.UserID == current)
                .Select(me => me.TeamID)
                .Distinct()
                .ToList();

            var myProjectIds = _context.MembreProjets
                .AsNoTracking()
                .Where(mp => mp.UserID == current)
                .Select(mp => mp.ProjectID)
                .Distinct()
                .ToList();

            // Option : inclure projets appartenant aux organisations des équipes (facultatif)
            var orgIdsFromTeams = _context.Equipes
                .AsNoTracking()
                .Where(e => myTeamIds.Contains(e.TeamID))
                .Select(e => e.OrgID)
                .Distinct()
                .ToList();

            var projectsFromOrgs = _context.Projets
                .AsNoTracking()
                .Where(p => orgIdsFromTeams.Contains(p.OrgID))
                .Select(p => p.ProjectID)
                .Distinct()
                .ToList();

            var effectiveProjectIds = myProjectIds.Union(projectsFromOrgs).Distinct().ToList();

            // Comptages réels
            var teamsCount = myTeamIds.Count;
            var projectsCount = effectiveProjectIds.Count;

            var tasksCount = _context.Taches
                .AsNoTracking()
                .Count(t =>
                    (t.AssigneeID.HasValue && t.AssigneeID.Value == current)
                    || (t.ProjectID.HasValue && effectiveProjectIds.Contains(t.ProjectID.Value))
                );

            // Récupérer données utilisateur pour affichage
            var user = _context.Utilisateurs
                .AsNoTracking()
                .FirstOrDefault(u => u.UserID == current);

            var vm = new ProfileViewModel
            {
                UserID = current,
                Nom = user?.Nom,
                Email = user?.Email,
                Avatar = user?.Avatar,
                Telephone = user?.Telephone,
                ProjectsCount = projectsCount,
                TasksCount = tasksCount,
                TeamsCount = teamsCount
            };

            return View("~/Views/Profile/Index.cshtml", vm);
        }

        // POST: /Profile (mise à jour inline des infos + upload avatar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4 * 1024 * 1024)] // limite safe (4MB) ; adjuster si besoin
        public async Task<IActionResult> Index(ProfileViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Données invalides.";
                return RedirectToAction("Index");
            }

            int userId;
            try
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch
            {
                TempData["Error"] = "Utilisateur non authentifié.";
                return RedirectToAction("Index");
            }

            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return NotFound();

            // traitement du fichier avatar (si fourni)
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext))
                {
                    TempData["Error"] = "Format d'image non autorisé. Utilisez jpg, png, gif ou webp.";
                    return RedirectToAction("Index");
                }

                const long maxBytes = 2 * 1024 * 1024; // 2MB
                if (avatarFile.Length > maxBytes)
                {
                    TempData["Error"] = "Image trop volumineuse (max 2 MB).";
                    return RedirectToAction("Index");
                }

                var webRoot = _env.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                {
                    _logger.LogWarning("WebRootPath null, fallback sur CurrentDirectory pour stockage.");
                    webRoot = Directory.GetCurrentDirectory();
                }

                var uploads = Path.Combine(webRoot, "uploads", "avatars");
                try
                {
                    Directory.CreateDirectory(uploads);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible de créer le dossier d'uploads {Uploads}", uploads);
                    TempData["Error"] = "Impossible de préparer le dossier de stockage.";
                    return RedirectToAction("Index");
                }

                var safeFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, safeFileName);

                try
                {
                    await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await avatarFile.CopyToAsync(stream);
                    await stream.FlushAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'écriture du fichier avatar pour l'utilisateur {UserId}", userId);
                    try { if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath); } catch { }
                    TempData["Error"] = "Erreur lors du téléversement du fichier.";
                    return RedirectToAction("Index");
                }

                // suppression ancienne image locale (silencieusement)
                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/uploads/avatars/"))
                {
                    try
                    {
                        var oldRel = user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var oldPath = Path.Combine(webRoot, oldRel);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de supprimer l'ancienne image avatar pour user {UserId}", userId);
                    }
                }

                user.Avatar = "/uploads/avatars/" + safeFileName;
            }
            else
            {
                // si l'utilisateur a renseigné manuellement une URL
                user.Avatar = model.Avatar;
            }

            user.Nom = model.Nom;
            user.Telephone = model.Telephone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du profil pour l'utilisateur {UserId}", user.UserID);
                TempData["Error"] = "Une erreur serveur est survenue lors de l'enregistrement.";
                return RedirectToAction("Index");
            }

            TempData["Message"] = "Profil mis à jour.";
            return RedirectToAction("Index");
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Tous les champs sont requis.";
                return RedirectToAction("Index");
            }

            if (newPassword.Length < 8)
            {
                TempData["Error"] = "Le nouveau mot de passe doit comporter au moins 8 caractères.";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "La confirmation du mot de passe ne correspond pas.";
                return RedirectToAction("Index");
            }

            int userId;
            try
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch
            {
                TempData["Error"] = "Utilisateur non authentifié.";
                return RedirectToAction("Index");
            }

            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            if (!PasswordHelper.Verify(user.MotDePasseHash, currentPassword))
            {
                TempData["Error"] = "Le mot de passe actuel est incorrect.";
                return RedirectToAction("Index");
            }

            user.MotDePasseHash = PasswordHelper.Hash(newPassword);
            _context.SaveChanges();

            TempData["Message"] = "Mot de passe mis à jour avec succès.";
            return RedirectToAction("Index");
        }

        // GET: /Profile/Edit -> redirige vers Index
        public IActionResult Edit() => RedirectToAction("Index");

        // POST: /Profile/Edit (support upload) — utilise la même logique que POST Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4 * 1024 * 1024)]
        public async Task<IActionResult> Edit(ProfileViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid) return View(model);

            int userId;
            try
            {
                userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            catch
            {
                return Challenge();
            }

            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return NotFound();

            // Reuse same upload logic
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext))
                {
                    TempData["Error"] = "Format d'image non autorisé.";
                    return RedirectToAction("Edit");
                }

                const long maxBytes = 2 * 1024 * 1024;
                if (avatarFile.Length > maxBytes)
                {
                    TempData["Error"] = "Image trop volumineuse (max 2 MB).";
                    return RedirectToAction("Edit");
                }

                var webRoot = _env.WebRootPath ?? Directory.GetCurrentDirectory();
                var uploads = Path.Combine(webRoot, "uploads", "avatars");
                try { Directory.CreateDirectory(uploads); } catch { }

                var safeFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, safeFileName);

                try
                {
                    await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                    await avatarFile.CopyToAsync(stream);
                    await stream.FlushAsync();
                }
                catch
                {
                    TempData["Error"] = "Erreur lors du téléversement du fichier.";
                    return RedirectToAction("Edit");
                }

                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/uploads/avatars/"))
                {
                    try
                    {
                        var oldRel = user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var oldPath = Path.Combine(webRoot, oldRel);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    catch { }
                }

                user.Avatar = "/uploads/avatars/" + safeFileName;
            }
            else
            {
                user.Avatar = model.Avatar;
            }

            user.Nom = model.Nom;
            user.Telephone = model.Telephone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du profil (Edit) pour l'utilisateur {UserId}", userId);
                TempData["Error"] = "Erreur serveur lors de l'enregistrement.";
                return RedirectToAction("Edit");
            }

            TempData["Message"] = "Profil mis à jour.";
            return RedirectToAction("Index");
        }
    }
}