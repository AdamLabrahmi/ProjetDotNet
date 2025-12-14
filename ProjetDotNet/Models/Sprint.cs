using System.ComponentModel.DataAnnotations;
using ProjetDotNet.Models.Enums;
namespace ProjetDotNet.Models
{
    public class Sprint
    {
        [Key]
        public int SprintID { get; set; }
        public string Nom { get; set; }
        public string Objectif { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public StatutSprint Statut { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int ProjectID { get; set; }
        public Projet Projet { get; set; }

        public ICollection<Tache> Taches { get; set; }
    }

}
