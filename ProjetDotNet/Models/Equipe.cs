using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    [Table("equipe")]
    public class Equipe
    {
        [Key]
        public int TeamID { get; set; }

        [Required]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK vers Organisations
        public int OrgID { get; set; }

        [ForeignKey("OrgID")]
        public Organisations? Organisation { get; set; }

        // Navigation - initialisée pour éviter les null refs et validation inutile
        [ValidateNever]
        public ICollection<MembreEquipe> Membres { get; set; } = new List<MembreEquipe>();
    }
}
