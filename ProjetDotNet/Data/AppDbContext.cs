using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Models;
using ProjetDotNet.Models.Enums;

namespace ProjetDotNet.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Organisations> Organisations { get; set; }
        public DbSet<Equipe> Equipes { get; set; }
        public DbSet<Projet> Projets { get; set; }
        public DbSet<Tache> Taches { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<MembreEquipe> MembreEquipes { get; set; }
        public DbSet<MembreProjet> MembreProjets { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Commentaire> Commentaires { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tables explicites
            modelBuilder.Entity<Utilisateur>().ToTable("utilisateur");
            modelBuilder.Entity<Organisations>().ToTable("organisation");
            modelBuilder.Entity<Equipe>().ToTable("equipe");
            modelBuilder.Entity<Projet>().ToTable("projet");
            modelBuilder.Entity<Tache>().ToTable("tache");
            modelBuilder.Entity<Sprint>().ToTable("sprint");
            modelBuilder.Entity<Admin>().ToTable("admin");
            modelBuilder.Entity<Notification>().ToTable("notification");
            modelBuilder.Entity<Commentaire>().ToTable("commentaire");


            modelBuilder.Entity<ProjetDotNet.Models.Tache>(entity =>
            {
                entity.ToTable("tache"); // déjà présent via [Table] mais explicite ici
                entity.HasKey(e => e.TacheID);
                entity.Property(e => e.TacheID).HasColumnName("tache_id");         // <-- adapter si nécessaire
                entity.Property(e => e.Titre).HasColumnName("titre");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.EstimationDur).HasColumnName("estimation_dur");
                entity.Property(e => e.TempsRestant).HasColumnName("temps_restant");
                entity.Property(e => e.DateCreation).HasColumnName("date_creation");
                entity.Property(e => e.DateMiseAJour).HasColumnName("date_mise_a_jour");
                entity.Property(e => e.DateResolution).HasColumnName("date_resolution");
                entity.Property(e => e.ProjectID).HasColumnName("project_id");
                entity.Property(e => e.SprintID).HasColumnName("sprint_id");
                entity.Property(e => e.AssigneeID).HasColumnName("assignee_id");
                entity.Property(e => e.CreateurID).HasColumnName("createur_id");
            });
            // Admin 1:1 -> Utilisateur
            modelBuilder.Entity<Admin>(builder =>
            {
                builder.HasKey(a => a.UserID);
                builder.HasOne(a => a.Utilisateur)
                       .WithOne(u => u.Admin)
                       .HasForeignKey<Admin>(a => a.UserID)
                       .OnDelete(DeleteBehavior.Cascade);
            });

            // Equipe -> Organisations (cascade)
            modelBuilder.Entity<Equipe>()
                .HasOne(e => e.Organisation)
                .WithMany(o => o.Equipes)
                .HasForeignKey(e => e.OrgID)
                .OnDelete(DeleteBehavior.Cascade);

            // Projet -> Organisations (cascade)
            modelBuilder.Entity<Projet>()
                .HasOne(p => p.Organisation)
                .WithMany(o => o.Projets)
                .HasForeignKey(p => p.OrgID)
                .OnDelete(DeleteBehavior.Cascade);

            // Enums (existant)
            modelBuilder.Entity<Projet>(builder =>
            {
                builder.Property(p => p.Statut)
                        .HasConversion<string>()
                        .HasMaxLength(50);
            });

            // Sprint.Statut
            modelBuilder.Entity<Sprint>(builder =>
            {
                builder.Property(s => s.Statut)
                        .HasConversion<string>()
                        .HasMaxLength(50);
            });

            // Tache enums
            modelBuilder.Entity<Tache>(builder =>
            {
                builder.Property(t => t.Type)
                        .HasConversion<string>()
                        .HasMaxLength(50);

                builder.Property(t => t.Priorite)
                       .HasConversion<string>()
                       .HasMaxLength(50);

                builder.Property(t => t.Statut)
                       .HasConversion<string>()
                       .HasMaxLength(50);
            });

            // MembreProjet.Role (RoleProjet)
            modelBuilder.Entity<MembreProjet>(builder =>
            {
                builder.Property(m => m.Role)
                       .HasConversion<string>()
                       .HasMaxLength(50);
            });

            //modelBuilder.Entity<MembreEquipe>(builder =>
            //{
            //    builder.Property("Role")
            //           .HasConversion<string>()
            //           .HasColumnType("int");
            //});

            modelBuilder.Entity<MembreEquipe>()
                          .Property(m => m.Role)
                          .HasConversion<string>()
                          .HasMaxLength(50);

            // Si d'autres enums existent (RoleEquipe, RoleProjet, etc.), mappez-les ici de la même façon.
        }
    }
}
