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
        public DbSet<Invitation> Invitations { get; set; }

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
            modelBuilder.Entity<Invitation>().ToTable("invitation");

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

            // Remplacer les mappings Tache par ce bloc unique et exact
            modelBuilder.Entity<Tache>(entity =>
            {
                entity.ToTable("tache");
                entity.HasKey(e => e.TacheID);

                // noms EXACTS des colonnes en base (adapter si besoin)
                entity.Property(e => e.TacheID).HasColumnName("tacheID");
                entity.Property(e => e.ProjectID).HasColumnName("projectID");
                entity.Property(e => e.SprintID).HasColumnName("sprintID");
                entity.Property(e => e.AssigneeID).HasColumnName("assigneeID");
                entity.Property(e => e.CreateurID).HasColumnName("createurID");

                entity.Property(e => e.Titre).HasColumnName("titre");
                entity.Property(e => e.Description).HasColumnName("description");

                // enums stockés en string
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50).HasColumnName("type");
                entity.Property(e => e.Priorite).HasConversion<string>().HasMaxLength(50).HasColumnName("priorite");
                entity.Property(e => e.Statut).HasConversion<string>().HasMaxLength(50).HasColumnName("statut");

                // colonnes spécifiques
                entity.Property(e => e.EstimationDur).HasColumnName("estimation_dur");
                entity.Property(e => e.TempsRestant).HasColumnName("tempsRestant");

                entity.Property(e => e.DateCreation).HasColumnName("dateCreation");
                entity.Property(e => e.DateMiseAJour).HasColumnName("dateMiseAJour");
                entity.Property(e => e.DateResolution).HasColumnName("dateResolution");
            });

            // Mapping de MembreEquipe : clé composite (userID, teamID) pour correspondre à la table existante
            modelBuilder.Entity<MembreEquipe>(entity =>
            {
                entity.ToTable("membre_equipe");

                // clé composite correspondant aux colonnes existantes
                entity.HasKey(m => new { m.UserID, m.TeamID });

                entity.Property(m => m.UserID).HasColumnName("userID");
                entity.Property(m => m.TeamID).HasColumnName("teamID");

                // Conversion robuste compatible avec les arbres d'expression :
                // stockage : enum.ToString()
                // lecture : si null/vides -> RoleEquipe.Membre, sinon Enum.Parse(ignoreCase: true)
                entity.Property(m => m.Role)
                      .HasConversion(
                          v => v.ToString(),
                          s => string.IsNullOrEmpty(s)
                                ? RoleEquipe.Membre
                                : (RoleEquipe)Enum.Parse(typeof(RoleEquipe), s, true)
                       )
                      .HasMaxLength(50)
                      .HasColumnName("role");

                entity.Property(m => m.DateAjout).HasColumnName("dateAjout");

                // relations
                entity.HasOne(m => m.Utilisateur)
                      .WithMany(u => u.Equipes)
                      .HasForeignKey(m => m.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Equipe)
                      .WithMany(e => e.Membres)
                      .HasForeignKey(m => m.TeamID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // mapping explicite pour MembreProjet
            modelBuilder.Entity<MembreProjet>(entity =>
            {
                entity.ToTable("membre_projet");
                entity.HasKey(m => new { m.UserID, m.ProjectID });

                entity.Property(m => m.UserID).HasColumnName("userID");
                entity.Property(m => m.ProjectID).HasColumnName("projectID");

                entity.Property(m => m.Role)
                      .HasConversion(v => v.ToString(),
                                     s => (RoleProjet)Enum.Parse(typeof(RoleProjet), s, true))
                      .HasMaxLength(50)
                      .HasColumnName("role");

                entity.Property(m => m.DateAjout).HasColumnName("dateAjout");

                // relations - utiliser les collections inverses existantes
                entity.HasOne(m => m.Utilisateur)
                      .WithMany(u => u.Projets)   // ICollection<MembreProjet> Projets dans Utilisateur
                      .HasForeignKey(m => m.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Projet)
                      .WithMany(p => p.Membres)   // ICollection<MembreProjet> Membres dans Projet
                      .HasForeignKey(m => m.ProjectID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

           // Si d'autres enums existent (RoleEquipe, RoleProjet, etc.), mappez-les ici de la même façon.
        }

        // Méthodes auxiliaires utilisées par HasConversion : méthode appelée dans l'expression (évite SwitchExpression)
        private static string MapRoleToDatabaseString(RoleEquipe role)
        {
            // adapter les chaînes aux valeurs attendues par la DB (respect de la casse)
            return role switch
            {
                RoleEquipe.Admin => "Admin",
                RoleEquipe.Membre => "Membre",
                RoleEquipe.ScrumMaster => "ScrumMaster",
                RoleEquipe.ProductOwner => "ProductOwner",
                RoleEquipe.Designer => "Designer",
                RoleEquipe.QA => "QA",
                RoleEquipe.Observateur => "Observateur",
                _ => role.ToString()
            };
        }

        private static RoleEquipe MapDatabaseStringToRole(string s)
        {
            if (string.IsNullOrEmpty(s))
                return RoleEquipe.Membre; // valeur par défaut sûre

            // comparer en respectant la casse exacte si nécessaire, sinon normaliser
            return s switch
            {
                "Admin" => RoleEquipe.Admin,
                "Membre" => RoleEquipe.Membre,
                "ScrumMaster" => RoleEquipe.ScrumMaster,
                "ProductOwner" => RoleEquipe.ProductOwner,
                "Designer" => RoleEquipe.Designer,
                "QA" => RoleEquipe.QA,
                "Observateur" => RoleEquipe.Observateur,
                _ => Enum.TryParse<RoleEquipe>(s, out var parsed) ? parsed : RoleEquipe.Membre
            };
        }
    }
}
