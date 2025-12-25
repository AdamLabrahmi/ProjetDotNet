using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Models.ViewModels;
using ProjetDotNet.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace ProjetDotNet.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Profile
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                UserID = user.UserID,
                Nom = user.Nom ?? "",
                Email = user.Email,
                Telephone = user.Telephone,
                Avatar = user.Avatar
            };

            return View(vm);
        }

        // POST: /Profile (mise à jour inline des infos + upload avatar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ProfileViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Données invalides.";
                return RedirectToAction("Index");
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            // Gérer l'upload si présent
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

                var uploads = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    avatarFile.CopyTo(stream);
                }

                // supprimer l'ancienne image si elle est locale (ex: /uploads/avatars/xxx)
                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/uploads/avatars/"))
                {
                    var oldRel = user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPath = Path.Combine(_env.WebRootPath, oldRel);
                    try
                    {
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    catch
                    {
                        // ne pas échouer si suppression impossible
                    }
                }

                user.Avatar = "/uploads/avatars/" + fileName;
            }
            else
            {
                // si l'utilisateur a renseigné manuellement une URL dans le champ Avatar (facultatif)
                user.Avatar = model.Avatar;
            }

            user.Nom = model.Nom;
            user.Telephone = model.Telephone;

            _context.SaveChanges();

            TempData["Message"] = "Profil mis à jour.";
            return RedirectToAction("Index");
        }

        // POST: /Profile/ChangePassword (inchangé)
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

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

            // Vérifier le mot de passe actuel (utilise ton helper de hash)
            if (!PasswordHelper.Verify(user.MotDePasseHash, currentPassword))
            {
                TempData["Error"] = "Le mot de passe actuel est incorrect.";
                return RedirectToAction("Index");
            }

            // Mettre à jour le hash
            user.MotDePasseHash = PasswordHelper.Hash(newPassword);
            _context.SaveChanges();

            TempData["Message"] = "Mot de passe mis à jour avec succès.";
            return RedirectToAction("Index");
        }

        // GET: /Profile/Edit -> redirige vers Index
        public IActionResult Edit()
        {
            return RedirectToAction("Index");
        }

        // POST: /Profile/Edit (support upload)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProfileViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Utilisateurs.Find(userId);
            if (user == null) return NotFound();

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

                var uploads = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    avatarFile.CopyTo(stream);
                }

                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/uploads/avatars/"))
                {
                    var oldRel = user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPath = Path.Combine(_env.WebRootPath, oldRel);
                    try
                    {
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    catch { }
                }

                user.Avatar = "/uploads/avatars/" + fileName;
            }
            else
            {
                user.Avatar = model.Avatar;
            }

            user.Nom = model.Nom;
            user.Telephone = model.Telephone;
            _context.SaveChanges();

            TempData["Message"] = "Profil mis à jour.";
            return RedirectToAction("Index");
        }
    }
}
