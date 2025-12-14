using System.ComponentModel.DataAnnotations;
using ProjetDotNet.Models.Enums;
namespace ProjetDotNet.Models
{
    public class Projet
    {
        [Key]
        public int ProjectID { get; set; }
        public string Nom { get; set; }
        public string CleProjet { get; set; }
        public string Description { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public StatutProjet Statut { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int OrgID { get; set; }
        public Organisation Organisation { get; set; }

        // Navigation
        public ICollection<MembreProjet> Membres { get; set; }
        public ICollection<Sprint> Sprints { get; set; }
        public ICollection<Tache> Taches { get; set; }
    }

}
