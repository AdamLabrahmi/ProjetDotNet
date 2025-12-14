using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    public class Admin
    {
        [Key, ForeignKey("Utilisateur")]
        public int UserID { get; set; }

        public Utilisateur Utilisateur { get; set; }
        public ICollection<Organisation> Organisations { get; set; }
    }

}
