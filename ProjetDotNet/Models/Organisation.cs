using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class Organisation
    {
        [Key]
        public int OrgID { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int AdminID { get; set; }
        public Admin Admin { get; set; }

        // Navigation
        public ICollection<Equipe> Equipes { get; set; }
        public ICollection<Projet> Projets { get; set; }
    }

}