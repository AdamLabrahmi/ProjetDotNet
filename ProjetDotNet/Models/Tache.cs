using System.ComponentModel.DataAnnotations;
using ProjetDotNet.Models.Enums;
namespace ProjetDotNet.Models
{
    public class Tache
    {
        [Key]
        public int TacheID { get; set; }
        public string Titre { get; set; }
        public string Description { get; set; }
        public TypeTache Type { get; set; }
        public PrioriteTache Priorite { get; set; }
        public StatutTache Statut { get; set; }
        public float EstimationDur { get; set; }
        public float TempsRestant { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateMiseAJour { get; set; }
        public DateTime? DateResolution { get; set; }

        // FK
        public int ProjectID { get; set; }
        public Projet Projet { get; set; }

        public int? SprintID { get; set; }
        public Sprint Sprint { get; set; }

        public int? AssigneeID { get; set; }
        public Utilisateur Assignee { get; set; }

        public int CreateurID { get; set; }
        public Utilisateur Createur { get; set; }

        public ICollection<Commentaire> Commentaires { get; set; }
        public ICollection<PieceJointe> PiecesJointes { get; set; }
    }

}
