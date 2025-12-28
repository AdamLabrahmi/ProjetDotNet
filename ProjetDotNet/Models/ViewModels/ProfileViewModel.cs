using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        public string? Nom { get; set; }

        public string? Email { get; set; }

        public string? Avatar { get; set; }

        // Nouveau : numéro de téléphone
        public string? Telephone { get; set; }

        // Comptages réels
        public int ProjectsCount { get; set; }
        public int TasksCount { get; set; }
        public int TeamsCount { get; set; }

    }
}
