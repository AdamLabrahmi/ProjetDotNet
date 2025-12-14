using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class Commentaire
    {
        [Key]
        public int CommentaireID { get; set; }
        public string Contenu { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }

        // FK
        public int TacheID { get; set; }
        public Tache Tache { get; set; }

        public int UserID { get; set; }
        public Utilisateur Utilisateur { get; set; }
    }

}
