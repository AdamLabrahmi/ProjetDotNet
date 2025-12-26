using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Models
{
    [Table("sprint")]
    public class Sprint : IValidatableObject
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

        [Required(ErrorMessage = "Le projet est obligatoire")]
        public int ProjectID { get; set; }

        [ForeignKey(nameof(ProjectID))]
        public Projet? Projet { get; set; }

        public ICollection<Tache> Taches { get; set; } = new List<Tache>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Vérifier que la date de début est strictement antérieure à la date de fin
            if (DateDebut >= DateFin)
            {
                yield return new ValidationResult(
                    "La date de début doit être strictement inférieure à la date de fin.",
                    new[] { nameof(DateDebut), nameof(DateFin) });
            }

            // Optionnel : vérifier que DateDebut/DateFin sont cohérentes avec le projet si disponible
            if (Projet != null && Projet.DateDebut.HasValue && Projet.DateFin.HasValue)
            {
                if (DateDebut < Projet.DateDebut.Value.Date || DateFin > Projet.DateFin.Value.Date)
                {
                    yield return new ValidationResult(
                        "Les dates du sprint doivent être à l'intérieur des dates du projet.",
                        new[] { nameof(DateDebut), nameof(DateFin) });
                }
            }
        }
    }
}