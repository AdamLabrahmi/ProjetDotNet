using ProjetDotNet.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class MembreEquipe
    {
        [Key]
        public int MembreEquipeID { get; set; }
        public int UserID { get; set; }
        public int TeamID { get; set; }

        public RoleEquipe Role { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.Now;

        public Utilisateur Utilisateur { get; set; }
        public Equipe Equipe { get; set; }
    }

}
