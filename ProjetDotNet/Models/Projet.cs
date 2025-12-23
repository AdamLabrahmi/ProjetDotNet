using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("projet")]
    public class Projet
    {
        [Key]
        public int ProjectID { get; set; }

        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(50)]
        public string CleProjet { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }   // rendu nullable pour correspondre à l'utilisation de '?.' dans la vue

        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }     // rendu nullable pour correspondre à l'utilisation de '?.' dans la vue

        [Required]
        public StatutProjet Statut { get; set; } = StatutProjet.EN_ATTENTE;

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK explicite
        public int OrgID { get; set; }

        [ForeignKey("OrgID")]
        public Organisations? Organisation { get; set; }

        // Navigation - initialisées pour éviter les problèmes de validation/null
        public ICollection<MembreProjet> Membres { get; set; } = new List<MembreProjet>();
        public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        public ICollection<Tache> Taches { get; set; } = new List<Tache>();
    }
}