using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("tache")]
    public class Tache
    {
        [Key]
        public int TacheID { get; set; }

        [Required]
        [StringLength(200)]
        public string Titre { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required]
        public TypeTache Type { get; set; } = TypeTache.TASK;

        [Required]
        public PrioriteTache Priorite { get; set; } = PrioriteTache.MOYENNE;

        [Required]
        public StatutTache Statut { get; set; } = StatutTache.A_FAIRE;

        // Estimations en heures (ou unité choisie)
        public float EstimationDur { get; set; } = 0f;
        public float TempsRestant { get; set; } = 0f;

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateMiseAJour { get; set; }
        public DateTime? DateResolution { get; set; }

        // ========================
        // Relations (FK explicites)
        // ========================

        // 🔗 Projet (obligatoire)
        [Required(ErrorMessage = "Veuillez sélectionner un projet")]
        public int? ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Projet? Projet { get; set; }

        // 🔗 Sprint (optionnel)
        public int? SprintID { get; set; }

        [ForeignKey("SprintID")]
        public Sprint? Sprint { get; set; }

        // 🔗 Assignee (optionnel)
        public int? AssigneeID { get; set; }

        [ForeignKey("AssigneeID")]
        public Utilisateur? Assignee { get; set; }

        // 🔗 Créateur (obligatoire)
        [Required]
        public int CreateurID { get; set; }

        [ForeignKey("CreateurID")]
        public Utilisateur? Createur { get; set; }

        // ========================
        // Collections
        // ========================
        public ICollection<Commentaire> Commentaires { get; set; } = new List<Commentaire>();
        public ICollection<PieceJointe> PiecesJointes { get; set; } = new List<PieceJointe>();
    }
}
