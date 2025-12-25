using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Pages.Invitations
{
    public class AcceptModel : PageModel
    {
        private readonly AppDbContext _context;
        public AcceptModel(AppDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = null!;

        public string? OrgName { get; set; }
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Message { get; set; }

        public void OnGet()
        {
            var invite = _context.Invitations.FirstOrDefault(i => i.Token == Token);
            if (invite == null || invite.ExpiresAt < DateTime.UtcNow || invite.Accepted)
            {
                IsValid = false;
                Error = "Invitation invalide ou expirée.";
                return;
            }

            IsValid = true;
            OrgName = _context.Organisations.Find(invite.OrgID)?.Nom;
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var invite = _context.Invitations.FirstOrDefault(i => i.Token == Token);
            if (invite == null || invite.ExpiresAt < DateTime.UtcNow || invite.Accepted)
            {
                return Page();
            }

            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                // Rediriger vers login avec returnUrl si nécessaire
                return RedirectToPage("/Auth/Login", new { returnUrl = Url.Page("/Invitations/Accept", null, new { token = Token }, Request.Scheme) });
            }

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Marquer comme accepté
            invite.Accepted = true;
            _context.Invitations.Update(invite);

            // Ajouter l'utilisateur à l'équipe si TeamID présent, sinon ajouter comme membre de l'organisation (MembreProjet/Eq)
            if (invite.TeamID.HasValue)
            {
                var membre = new MembreEquipe
                {
                    UserID = currentUserId,
                    TeamID = invite.TeamID.Value,
                    Role = invite.Role,
                    DateAjout = DateTime.UtcNow
                };
                _context.MembreEquipes.Add(membre);
            }
            else
            {
                // Optionnel : ajouter des enregistrements organisation <-> utilisateur selon votre modèle
                // Ici on crée une équipe "Par défaut" ou on peut ajouter au Projet membres etc.
            }

            await _context.SaveChangesAsync();

            Message = "Invitation acceptée — vous avez été ajouté(e).";
            return Page();
        }
    }
}