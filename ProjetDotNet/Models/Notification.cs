using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models
{
    public class Notification
    {
        [Key]
        public int NotifID { get; set; }
        public string Contenu { get; set; }
        public bool Lu { get; set; } = false;
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // FK
        public int UserID { get; set; }
        public Utilisateur Utilisateur { get; set; }
    }

}
