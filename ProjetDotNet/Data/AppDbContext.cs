using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Models; 

namespace ProjetDotNet.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<Equipe> Equipes { get; set; }
        public DbSet<Projet> Projets { get; set; }
        public DbSet<Tache> Taches { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<Sprint> MembreEquipe { get; set; }
        public DbSet<Sprint> MembreProjet { get; set; }
        public DbSet<Sprint> Admin { get; set; }
        public DbSet<Sprint> Commentaire { get; set; }
        public DbSet<Sprint> Notification { get; set; }
    }
}
