using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("projet")]
    public class Projet : IValidatableObject
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
        public DateTime? DateDebut { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        [Required]
        public StatutProjet Statut { get; set; } = StatutProjet.EN_ATTENTE;

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public int OrgID { get; set; }

        [ForeignKey("OrgID")]
        public Organisations? Organisation { get; set; }

        public ICollection<MembreProjet> Membres { get; set; } = new List<MembreProjet>();
        public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        public ICollection<Tache> Taches { get; set; } = new List<Tache>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Si les deux dates sont renseignées, vérifier l'ordre
            if (DateDebut.HasValue && DateFin.HasValue)
            {
                if (DateDebut.Value.Date >= DateFin.Value.Date)
                {
                    yield return new ValidationResult(
                        "La date de début doit être strictement antérieure à la date de fin.",
                        new[] { nameof(DateDebut), nameof(DateFin) });
                }
            }
        }
    }
}