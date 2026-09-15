using System.Text.Json;
using LegalManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LegalManager.Infrastructure.Persistence
{
    public class LegalManagerDbContext : DbContext
    {
        public LegalManagerDbContext(DbContextOptions<LegalManagerDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Case> Cases => Set<Case>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Lawyer>().ToTable("Lawyers");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            modelBuilder.Entity<Lawyer>()
                .Property(l => l.Specialties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v.ToList()),
                    v => (IReadOnlyList<string>)(JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>()))
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            modelBuilder.Entity<Case>()
                .Ignore(c => c.LawyerIds);

            modelBuilder.Entity<Case>()
                .HasMany<Lawyer>("lawyers")
                .WithMany()
                .UsingEntity(
                    "CaseLawyer",
                    r => r.HasOne(typeof(Lawyer)).WithMany().HasForeignKey("LawyerId").OnDelete(DeleteBehavior.Restrict),
                    l => l.HasOne(typeof(Case)).WithMany().HasForeignKey("CaseId").OnDelete(DeleteBehavior.Restrict),
                    j => j.HasKey("CaseId", "LawyerId"));

            modelBuilder.Entity<Case>()
                .Navigation("lawyers")
                .AutoInclude();

            modelBuilder.Entity<Case>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Case>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .Ignore(a => a.EffectiveStatus);

            modelBuilder.Entity<Appointment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne<Case>()
                .WithMany()
                .HasForeignKey(a => a.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne<Case>()
                .WithMany()
                .HasForeignKey(d => d.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}