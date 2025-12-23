using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    [Table("admin")]
    public class Admin
    {
        [Key, ForeignKey("Utilisateur")]
        public int UserID { get; set; }

        public Utilisateur Utilisateur { get; set; }
        public ICollection<Organisations> Organisations { get; set; }
    }

}
