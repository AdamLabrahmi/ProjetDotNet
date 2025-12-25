using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("membre_equipe")]
    public class MembreEquipe
    {
        // 🔗 USER
        [Column("userID")]
        public int UserID { get; set; }

        [ForeignKey(nameof(UserID))]
        public Utilisateur Utilisateur { get; set; }

        // 🔗 TEAM
        [Column("teamID")]
        public int TeamID { get; set; }

        [ForeignKey(nameof(TeamID))]
        public Equipe Equipe { get; set; }

        // 🔗 ROLE
        [Column("role")]
        public RoleEquipe Role { get; set; } = RoleEquipe.Membre;

        // 🔗 DATE
        [Column("dateAjout")]
        public DateTime DateAjout { get; set; } = DateTime.Now;
    }
}
