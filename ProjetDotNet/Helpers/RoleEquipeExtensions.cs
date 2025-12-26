using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Helpers
{
    public static class RoleEquipeExtensions
    {
        public static string ToLabel(this RoleEquipe role)
        {
            return role switch
            {
                RoleEquipe.Admin => "Admin",
                RoleEquipe.Membre => "Membre",
                RoleEquipe.ScrumMaster => "ScrumMaster",
                RoleEquipe.ProductOwner => "ProductOwner",
                RoleEquipe.Designer => "Designer",
                RoleEquipe.QA => "QA",
                RoleEquipe.Observateur => "Observateur",
                _ => role.ToString()
            };
        }
    }
}
