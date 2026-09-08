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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH para la herencia de User (Client, Lawyer, Admin)
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Client>(UserType.Client)
                .HasValue<Lawyer>(UserType.Lawyer)
                .HasValue<Admin>(UserType.Admin);

            // Specialties del abogado como JSON
            modelBuilder.Entity<Lawyer>()
                .Property(l => l.Specialties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v.ToList()),
                    v => (IReadOnlyList<string>)(JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>()))
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            // Case: ignore propiedades calculadas
            modelBuilder.Entity<Case>()
                .Ignore(c => c.LawyerIds);

            // Case ↔ Lawyer (many-to-many, navegacion shadow "lawyers")
            modelBuilder.Entity<Case>()
                .HasMany<Lawyer>("lawyers")
                .WithMany();

            modelBuilder.Entity<Case>()
                .Navigation("lawyers")
                .AutoInclude();

            // Case → Client
            modelBuilder.Entity<Case>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Case → CreatedBy
            modelBuilder.Entity<Case>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment: ignore propiedades calculadas
            modelBuilder.Entity<Appointment>()
                .Ignore(a => a.EffectiveStatus);

            // Appointment → Client
            modelBuilder.Entity<Appointment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment → Lawyer
            modelBuilder.Entity<Appointment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment → Case
            modelBuilder.Entity<Appointment>()
                .HasOne<Case>()
                .WithMany()
                .HasForeignKey(a => a.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}