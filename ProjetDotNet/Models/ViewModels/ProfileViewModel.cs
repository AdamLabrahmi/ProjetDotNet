using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string? Telephone { get; set; }
        public string? Avatar { get; set; }
        public DateTime? DateInscription { get; set; }
    }
}
