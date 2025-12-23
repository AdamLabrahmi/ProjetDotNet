using ProjetDotNet.Models.Enums;
using System.ComponentModel.DataAnnotations;
namespace ProjetDotNet.Models
{
    public class MembreProjet
    {
        [Key]
        public int MembreProjetID { get; set; }
        // clé composite configurée dans DbContext
        public int UserID { get; set; }
        public int ProjectID { get; set; }

        public RoleProjet Role { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.Now;

        public Utilisateur Utilisateur { get; set; }
        public Projet Projet { get; set; }
    }

}
