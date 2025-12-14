namespace ProjetDotNet.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Utilisateur
    {
        [Key]
        public int UserID { get; set; }

        public string Nom { get; set; }
        public string Email { get; set; }
        public string MotDePasseHash { get; set; }
        public string Telephone { get; set; }
        public string Avatar { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation
        public Admin Admin { get; set; }
        public ICollection<MembreEquipe> Equipes { get; set; }
        public ICollection<MembreProjet> Projets { get; set; }
        public ICollection<Tache> TachesAssignées { get; set; }
        public ICollection<Tache> TachesCréées { get; set; }
        public ICollection<Commentaire> Commentaires { get; set; }
        public ICollection<PieceJointe> PiecesJointes { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }

}