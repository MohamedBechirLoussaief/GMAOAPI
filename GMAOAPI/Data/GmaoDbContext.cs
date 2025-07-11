using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using System.Reflection.Emit;

namespace GMAOAPI.Data
{
    public class GmaoDbContext : IdentityDbContext<Utilisateur>
    {
        public GmaoDbContext(DbContextOptions<GmaoDbContext> options)
            : base(options)
        {
        }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Equipement> Equipements { get; set; }
        public DbSet<Intervention> Interventions { get; set; }
        public DbSet<PieceDetachee> PiecesDetachees { get; set; }
        public DbSet<Planification> Planifications { get; set; }
        public DbSet<Rapport> Rapports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Fournisseur> Fournisseurs { get; set; }
        public DbSet<InterventionTechnicien> InterventionTechniciens { get; set; }
        public DbSet<InterventionPieceDetachee> InterventionPieceDetachees { get; set; }
        public DbSet<Audit> Audits { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<InterventionPieceDetachee>()
                .HasKey(ipd => new { ipd.InterventionId, ipd.PieceDetacheeId });

            builder.Entity<InterventionPieceDetachee>()
                .HasOne(ipd => ipd.Intervention)
                .WithMany(i => i.InterventionPieceDetachees)
                .HasForeignKey(ipd => ipd.InterventionId);

            builder.Entity<InterventionPieceDetachee>()
                .HasOne(ipd => ipd.PieceDetachee)
                .WithMany(pd => pd.InterventionPieceDetachees)
                .HasForeignKey(ipd => ipd.PieceDetacheeId);

            builder.Entity<InterventionTechnicien>()
                .HasKey(it => new { it.TechnicienId, it.InterventionId });

            builder.Entity<InterventionTechnicien>()
                .HasOne(it => it.Intervention)
                .WithMany(i => i.InterventionTechniciens)
                .HasForeignKey(it => it.InterventionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InterventionTechnicien>()
                .HasOne(it => it.Technicien)
                .WithMany(t => t.InterventionTechniciens)
                .HasForeignKey(it => it.TechnicienId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Intervention>()
                .HasOne(i => i.Rapport)
                .WithOne(r => r.Intervention)
                .HasForeignKey<Rapport>(r => r.InterventionId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Utilisateur>()
                .HasDiscriminator<RoleUtilisateur>("RoleUtilisateur")
                .HasValue<Technicien>(RoleUtilisateur.Technicien)
                .HasValue<Admin>(RoleUtilisateur.Admin)
                .HasValue<Responsable>(RoleUtilisateur.Responsable);

            builder.Entity<Rapport>()
              .HasOne(r => r.Createur)
              .WithMany(u => u.RapportsCrees)
              .HasForeignKey(r => r.CreateurId)
              .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rapport>()
                 .HasOne(r => r.Valideur)
                 .WithMany(u => u.RapportsValidees)
                 .HasForeignKey(r => r.ValideurId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Equipement>()
            .HasIndex(e => e.Reference)
            .IsUnique();

            builder.Entity<PieceDetachee>()
            .HasIndex(e => e.Reference)
            .IsUnique();

            builder.Entity<IdentityRole>().HasData(
               new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
               new IdentityRole { Id = "2", Name = "Technicien", NormalizedName = "TECHNICIEN" },
               new IdentityRole { Id = "3", Name = "Responsable", NormalizedName = "RESPONSABLE" }
           );
            builder.Entity<Utilisateur>().Ignore(u => u.PhoneNumber);
            builder.Entity<Utilisateur>().Ignore(u => u.PhoneNumberConfirmed);

            builder
            .Entity<Intervention>()
            .Property(i => i.Type)
            .HasConversion<string>()    
            .HasMaxLength(50);

            builder.Entity<Intervention>()
                .Property(i => i.Statut)
            .HasConversion<string>()
            .HasMaxLength(50);

            builder.Entity<Intervention>()
              .Property(i => i.AnnulationReason)
          .HasConversion<string>()
          .HasMaxLength(50);

            builder.Entity<Intervention>()
              .Property(i => i.ArchiveReason)
          .HasConversion<string>()
          .HasMaxLength(50);

            builder.Entity<Equipement>()
             .Property(i => i.Etat)
         .HasConversion<string>()
         .HasMaxLength(50);

            builder.Entity<Notification>()
             .Property(i => i.Statut)
         .HasConversion<string>()
         .HasMaxLength(50);

            builder.Entity<Planification>()
             .Property(i => i.Frequence)
         .HasConversion<string>()
         .HasMaxLength(50);
            builder.Entity<Planification>()
             .Property(i => i.ArchiveReason)
         .HasConversion<string>()
         .HasMaxLength(50);
            builder.Entity<Rapport>()
             .Property(i => i.Resultat)
         .HasConversion<string>()
         .HasMaxLength(50);
            builder.Entity<Technicien>()
             .Property(i => i.Specialite)
         .HasConversion<string>()
         .HasMaxLength(50);

            builder.Entity<Utilisateur>()
             .Property(i => i.RoleUtilisateur)
         .HasConversion<string>()
         .HasMaxLength(50);

        }


    }
}
