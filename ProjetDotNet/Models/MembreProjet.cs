using System;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("membre_projet")]
    public class MembreProjet
    {
        [Column("userID")]
        public int UserID { get; set; }

        [Column("projectID")]
        public int ProjectID { get; set; }

        [Column("role")]
        public RoleProjet Role { get; set; } = RoleProjet.Contributeur;

        [Column("dateAjout")]
        public DateTime DateAjout { get; set; } = DateTime.Now;

        // navigations (rendre nullable pour éviter problèmes si pas chargées)
        [ForeignKey(nameof(UserID))]
        public Utilisateur? Utilisateur { get; set; }

        [ForeignKey(nameof(ProjectID))]
        public Projet? Projet { get; set; }
    }
}
