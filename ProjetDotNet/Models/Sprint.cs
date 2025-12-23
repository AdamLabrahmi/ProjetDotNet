using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("sprint")]
    public class Sprint
    {
        [Key]
        public int SprintID { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        public string? Objectif { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateDebut { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateFin { get; set; }

        [Required]
        public StatutSprint Statut { get; set; } = StatutSprint.PLANIFIE;

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Clé étrangère vers Projet (obligatoire)
        [Required(ErrorMessage = "Le projet est obligatoire")]
        public int ProjectID { get; set; }  // Changé de int? à int pour forcer la non-nullabilité

        // Navigation vers Projet — rendu nullable pour éviter la validation automatique
        [ForeignKey(nameof(ProjectID))]
        public Projet? Projet { get; set; }  // Changé de Projet à Projet? et retiré = null!

        public ICollection<Tache> Taches { get; set; } = new List<Tache>();
    }
}