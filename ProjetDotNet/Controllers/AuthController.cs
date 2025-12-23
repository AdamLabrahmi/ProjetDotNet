using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Helpers;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjetDotNet.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Utilisateur user, string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("", "Email requis");
                return View();
            }

            if (user.MotDePasseHash != ConfirmPassword)
            {
                ModelState.AddModelError("", "Les mots de passe ne correspondent pas");
                return View();
            }

            bool emailExiste = _context.Utilisateurs
                .Any(u => u.Email.ToLower() == user.Email.ToLower());

            if (emailExiste)
            {
                ModelState.AddModelError("", "Email déjà utilisé");
                return View();
            }

            user.MotDePasseHash = PasswordHelper.Hash(user.MotDePasseHash);
            _context.Utilisateurs.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // GET: /Auth/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email et mot de passe requis");
                return View();
            }

            var user = _context.Utilisateurs.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user == null || !PasswordHelper.Verify(user.MotDePasseHash, password))
            {
                ModelState.AddModelError("", "Identifiants invalides");
                return View();
            }

            // Construire les claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Nom ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Auth/Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
