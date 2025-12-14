using ProjetDotNet.Models.Enums;
namespace ProjetDotNet.Models
{
    public class MembreProjet
    {
        // clé composite configurée dans DbContext
        public int UserID { get; set; }
        public int ProjectID { get; set; }

        public RoleProjet Role { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.Now;

        public Utilisateur Utilisateur { get; set; }
        public Projet Projet { get; set; }
    }

}
