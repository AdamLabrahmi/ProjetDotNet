using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class PieceJointe
    {
        [Key]
        public int FichierID { get; set; }
        public string NomFichier { get; set; }
        public string UrlFichier { get; set; }
        public DateTime DateUpload { get; set; } = DateTime.Now;

        // FK
        public int TacheID { get; set; }
        public Tache Tache { get; set; }

        public int UserID { get; set; }
        public Utilisateur Utilisateur { get; set; }
    }

}
