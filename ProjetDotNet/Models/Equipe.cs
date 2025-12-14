using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class Equipe
    {
        [Key]
        public int TeamID { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int OrgID { get; set; }
        public Organisation Organisation { get; set; }

        // Navigation
        public ICollection<MembreEquipe> Membres { get; set; }
    }

}
