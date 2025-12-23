using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    [Table("organisation")]
    public class Organisations
    {
        [Key]
        public int OrgID { get; set; }

        public string Nom { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int AdminID { get; set; }

        [ForeignKey("AdminID")]
        public Admin? Admin { get; set; }

        // Navigation - initialisées pour éviter les erreurs de validation
        public ICollection<Equipe> Equipes { get; set; } = new List<Equipe>();
        public ICollection<Projet> Projets { get; set; } = new List<Projet>();
    }
}