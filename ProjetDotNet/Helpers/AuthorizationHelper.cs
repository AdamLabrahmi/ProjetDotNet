using System;
using System.Linq;
using System.Security.Claims;
using ProjetDotNet.Data;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Helpers
{
    public static class AuthorizationHelper
    {
        public static int GetCurrentUserId(ClaimsPrincipal user, AppDbContext ctx)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var idFromClaim))
                return idFromClaim;

            var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name;
            if (!string.IsNullOrEmpty(email))
            {
                var u = ctx.Utilisateurs.FirstOrDefault(x => x.Email == email);
                if (u != null) return u.UserID;
            }

            return 0;
        }

        public static bool IsSiteAdmin(AppDbContext ctx, int userId)
        {
            if (userId == 0) return false;
            return ctx.Admins.Any(a => a.UserID == userId);
        }

        public static bool IsTeamAdmin(AppDbContext ctx, int userId, int teamId)
        {
            if (userId == 0) return false;
            return ctx.MembreEquipes.Any(me =>
                me.TeamID == teamId &&
                me.UserID == userId &&
                me.Role == RoleEquipe.Admin);
        }

        // NOUVEAU : vérifie si l'utilisateur est Admin d'au moins une équipe
        public static bool IsAnyTeamAdmin(AppDbContext ctx, int userId)
        {
            if (userId == 0) return false;
            return ctx.MembreEquipes.Any(me => me.UserID == userId && me.Role == RoleEquipe.Admin);
        }

        public static bool IsScrumMasterOfTeam(AppDbContext ctx, int userId, int teamId)
        {
            if (userId == 0) return false;
            return ctx.MembreEquipes.Any(me =>
                me.TeamID == teamId &&
                me.UserID == userId &&
                me.Role == RoleEquipe.ScrumMaster);
        }

        // Seuls SiteAdmin, Team Admin ou Team ScrumMaster peuvent ajouter des membres à une équipe
        public static bool CanAddMembersToTeam(AppDbContext ctx, int userId, int teamId)
        {
            if (userId == 0) return false;
            if (IsSiteAdmin(ctx, userId)) return true;
            if (IsTeamAdmin(ctx, userId, teamId)) return true;
            return IsScrumMasterOfTeam(ctx, userId, teamId);
        }

        // Seuls SiteAdmin, Project Admin ou Project ScrumMaster peuvent ajouter des membres au projet
        public static bool CanAddMembersToProject(AppDbContext ctx, int userId, int projectId)
        {
            if (userId == 0) return false;
            if (IsSiteAdmin(ctx, userId)) return true;

            // vérifier dans MembreProjets : seuls RoleProjet.Administrateur ou RoleProjet.ScrumMaster autorisés
            return ctx.MembreProjets.Any(mp =>
                mp.ProjectID == projectId &&
                mp.UserID == userId &&
                (mp.Role == RoleProjet.Administrateur || mp.Role == RoleProjet.ScrumMaster));
        }

        // Qui peut gérer les membres (Admin, ScrumMaster, ProductOwner, SiteAdmin)
        public static bool CanManageTeamMembers(AppDbContext ctx, int userId, int teamId)
        {
            if (userId == 0) return false;
            if (IsSiteAdmin(ctx, userId)) return true;
            var allowedRoles = new[] { RoleEquipe.Admin, RoleEquipe.ScrumMaster, RoleEquipe.ProductOwner };
            return ctx.MembreEquipes.Any(me =>
                me.TeamID == teamId &&
                me.UserID == userId &&
                allowedRoles.Contains(me.Role));
        }

        // NOUVELLE MÉTHODE : vérifie si l'utilisateur est administrateur du projet
        public static bool IsProjectAdmin(AppDbContext ctx, int userId, int projectId)
        {
            if (userId == 0) return false;
            // Site admin always allowed
            if (IsSiteAdmin(ctx, userId)) return true;

            // Check explicit project membership with admin role
            return ctx.MembreProjets.Any(mp =>
                mp.ProjectID == projectId &&
                mp.UserID == userId &&
                mp.Role == RoleProjet.Administrateur);
        }

        public static bool IsScrumMasterOfProject(AppDbContext ctx, int userId, int projectId)
        {
            if (userId == 0) return false;
            var orgId = ctx.Projets.Where(p => p.ProjectID == projectId).Select(p => p.OrgID).FirstOrDefault();
            if (orgId == 0) return false;
            var teamIds = ctx.Equipes.Where(e => e.OrgID == orgId).Select(e => e.TeamID).ToList();
            if (!teamIds.Any()) return false;
            return ctx.MembreEquipes.Any(me =>
                teamIds.Contains(me.TeamID) &&
                me.UserID == userId &&
                me.Role == RoleEquipe.ScrumMaster);
        }

        //public static bool IsAnyTeamAdmin(AppDbContext ctx, int userId)
        //{
        //    if (userId == 0) return false;
        //    return ctx.MembreEquipes.Any(me => me.UserID == userId && me.Role == RoleEquipe.Admin);
        //}

        //public static bool IsScrumMasterOfTeam(AppDbContext ctx, int userId, int teamId)
        //{
        //    if (userId == 0) return false;
        //    return ctx.MembreEquipes.Any(me =>
        //        me.TeamID == teamId &&
        //        me.UserID == userId &&
        //        me.Role == RoleEquipe.ScrumMaster);
        //}

        public static bool CanCreateSprint(AppDbContext ctx, int userId, int projectId)
        {
            if (userId == 0) return false;
            if (IsSiteAdmin(ctx, userId)) return true;

            var orgId = ctx.Projets.Where(p => p.ProjectID == projectId).Select(p => p.OrgID).FirstOrDefault();
            if (orgId == 0) return false;

            var teamIds = ctx.Equipes.Where(e => e.OrgID == orgId).Select(e => e.TeamID).ToList();
            if (!teamIds.Any()) return false;

            return ctx.MembreEquipes.Any(me =>
                teamIds.Contains(me.TeamID) &&
                me.UserID == userId &&
                (me.Role == RoleEquipe.Admin || me.Role == RoleEquipe.ScrumMaster));
        }

        public static bool CanCreateTache(AppDbContext ctx, int userId, int projectId)
        {
            if (userId == 0) return false;
            if (IsSiteAdmin(ctx, userId)) return true;

            // Vérifier rôle explicite sur le projet (MembreProjet)
            var hasProjectRole = ctx.MembreProjets.Any(mp =>
                mp.ProjectID == projectId &&
                mp.UserID == userId &&
                (mp.Role == RoleProjet.ProductOwner || mp.Role == RoleProjet.ScrumMaster || mp.Role == RoleProjet.Testeur));
            if (hasProjectRole) return true;

            // Sinon vérifier rôles d'équipe sur équipes de l'orga
            var orgId = ctx.Projets.Where(p => p.ProjectID == projectId).Select(p => p.OrgID).FirstOrDefault();
            if (orgId == 0) return false;

            var teamIds = ctx.Equipes.Where(e => e.OrgID == orgId).Select(e => e.TeamID).ToList();
            if (!teamIds.Any()) return false;

            return ctx.MembreEquipes.Any(me =>
                teamIds.Contains(me.TeamID) &&
                me.UserID == userId &&
                (me.Role == RoleEquipe.ScrumMaster || me.Role == RoleEquipe.ProductOwner || me.Role == RoleEquipe.QA));
        }

    }
}