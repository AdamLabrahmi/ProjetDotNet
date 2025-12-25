using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetDotNet.Models
{
    [Table("utilisateur")]
    public class Utilisateur
    {
        [Key]
        public int UserID { get; set; }

        public string? Nom { get; set; }
        public string Email { get; set; } = null!;
        public string MotDePasseHash { get; set; } = null!;

        public string? Telephone { get; set; }
        public string? Avatar { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation
        public Admin Admin { get; set; }

        // corrected: inverse property points to the navigation on MembreEquipe
        //[InverseProperty("Utilisateur")]
        //public ICollection<MembreEquipe> Equipes { get; set; }

        //public ICollection<MembreProjet> Projets { get; set; }

        //[InverseProperty("Assignee")]
        //public ICollection<Tache> TachesAssignées { get; set; }

        //[InverseProperty("Createur")]
        //public ICollection<Tache> TachesCréées { get; set; }

        // inverse property vers MembreEquipe
        [InverseProperty("Utilisateur")]
        public ICollection<MembreEquipe> Equipes { get; set; } = new List<MembreEquipe>();

        public ICollection<MembreProjet> Projets { get; set; } = new List<MembreProjet>();

        [InverseProperty("Assignee")]
        public ICollection<Tache> TachesAssignées { get; set; } = new List<Tache>();

        [InverseProperty("Createur")]
        public ICollection<Tache> TachesCréées { get; set; } = new List<Tache>();


        public ICollection<Commentaire> Commentaires { get; set; }
        public ICollection<PieceJointe> PiecesJointes { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}