using DeliveryApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Livreur> Livreurs { get; set; }
        public DbSet<Vehicule> Vehicules { get; set; }
        public DbSet<Camion> Camions { get; set; }
        public DbSet<Colis> Colis { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── TPH pour Vehicule ───────────────────────────────────────────
            modelBuilder.Entity<Vehicule>()
                .HasDiscriminator<string>("TypeVehicule")
                .HasValue<Camion>("Camion")
                .HasValue<Voiture>("Voiture");

            // ─── Client ──────────────────────────────────────────────────────
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Nom)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Prenom)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.CodePostal)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(c => c.Ville)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Email)
                    .HasMaxLength(200);

                entity.HasIndex(c => c.Email)
                    .IsUnique()
                    .HasFilter("\"Email\" IS NOT NULL");
            });

            // ─── Livreur ─────────────────────────────────────────────────────
            modelBuilder.Entity<Livreur>(entity =>
            {
                entity.ToTable("Livreurs");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.CIN)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(l => l.CIN).IsUnique();

                entity.Property(l => l.RaisonSociale)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(l => l.CodePostal)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(l => l.Ville)
                    .IsRequired()
                    .HasMaxLength(100);

                // Relation Livreur -> Véhicules (un livreur possède plusieurs véhicules)
                entity.HasMany(l => l.Vehicules)
                    .WithOne(v => v.Livreur)
                    .HasForeignKey(v => v.LivreurId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── Vehicule ────────────────────────────────────────────────────
            modelBuilder.Entity<Vehicule>(entity =>
            {
                entity.ToTable("Vehicules");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.Matricule)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(v => v.Matricule).IsUnique();

                entity.Property(v => v.Marque)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(v => v.Couleur)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            // ─── Colis ───────────────────────────────────────────────────────
            modelBuilder.Entity<Colis>(entity =>
            {
                entity.ToTable("Colis");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Montant)
                    .HasColumnType("decimal(18,2)");

                entity.Property(c => c.Poids)
                    .HasColumnType("decimal(10,3)");

                entity.Property(c => c.Volume)
                    .HasColumnType("decimal(10,3)");

                entity.Property(c => c.Statut)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Colis -> Client
                entity.HasOne(c => c.Client)
                    .WithMany(cl => cl.Colis)
                    .HasForeignKey(c => c.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Colis -> Livreur (optionnel)
                entity.HasOne(c => c.Livreur)
                    .WithMany(l => l.Colis)
                    .HasForeignKey(c => c.LivreurId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
