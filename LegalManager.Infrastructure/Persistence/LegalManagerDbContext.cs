using System.Text.Json;
using LegalManager.Domain;
using LegalManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LegalManager.Infrastructure.Persistence
{
    public class LegalManagerDbContext(DbContextOptions<LegalManagerDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Case> Cases => Set<Case>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().UseTpcMappingStrategy();
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Lawyer>().ToTable("Lawyers");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            // Without this all text stays nvarchar(max). The limits live in Domain.FieldLengths
            // so that this layer and the DTOs' [StringLength] cannot diverge.
            modelBuilder.Entity<User>().Property(u => u.FirstName).HasMaxLength(FieldLengths.PersonName);
            modelBuilder.Entity<User>().Property(u => u.LastName).HasMaxLength(FieldLengths.PersonName);
            modelBuilder.Entity<User>().Property(u => u.Dni).HasMaxLength(FieldLengths.Dni);
            modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(FieldLengths.Email);
            modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasMaxLength(FieldLengths.PasswordHash);

            modelBuilder.Entity<Client>().Property(c => c.Phone).HasMaxLength(FieldLengths.Phone);
            modelBuilder.Entity<Client>().Property(c => c.Address).HasMaxLength(FieldLengths.Address);

            // La unicidad de Email y Dni la controla UserService en memoria, pero con TPC no hay
            // tabla base para un constraint unico global: estos indices por tabla respaldan esa
            // unicidad dentro de cada una y cierran la carrera de dos requests simultaneos.
            // El filtro por [Active] = 1 alinea el indice con EnsureUnique (que solo mira
            // usuarios activos): el email/DNI de un usuario dado de baja queda libre para
            // reutilizarse en vez de chocar contra una fila inactiva.
            modelBuilder.Entity<Client>().HasIndex(c => c.Email).IsUnique().HasDatabaseName("UX_Clients_Email").HasFilter("[Active] = 1");
            modelBuilder.Entity<Client>().HasIndex(c => c.Dni).IsUnique().HasDatabaseName("UX_Clients_Dni").HasFilter("[Active] = 1");
            modelBuilder.Entity<Lawyer>().HasIndex(l => l.Email).IsUnique().HasDatabaseName("UX_Lawyers_Email").HasFilter("[Active] = 1");
            modelBuilder.Entity<Lawyer>().HasIndex(l => l.Dni).IsUnique().HasDatabaseName("UX_Lawyers_Dni").HasFilter("[Active] = 1");
            modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique().HasDatabaseName("UX_Admins_Email").HasFilter("[Active] = 1");
            modelBuilder.Entity<Admin>().HasIndex(a => a.Dni).IsUnique().HasDatabaseName("UX_Admins_Dni").HasFilter("[Active] = 1");

            modelBuilder.Entity<Lawyer>().Property(l => l.Phone).HasMaxLength(FieldLengths.Phone);
            modelBuilder.Entity<Lawyer>().Property(l => l.BarNumber).HasMaxLength(FieldLengths.BarNumber);
            // Specialties stays nvarchar(max) on purpose: it is serialized JSON, not a string.

            modelBuilder.Entity<Case>().Property(c => c.CaseNumber).HasMaxLength(FieldLengths.CaseNumber);
            modelBuilder.Entity<Case>().Property(c => c.Title).HasMaxLength(FieldLengths.Title);
            modelBuilder.Entity<Case>().Property(c => c.Area).HasMaxLength(FieldLengths.Area);
            modelBuilder.Entity<Case>().Property(c => c.Description).HasMaxLength(FieldLengths.LongText);
            modelBuilder.Entity<Case>().Property(c => c.Notes).HasMaxLength(FieldLengths.LongText);

            modelBuilder.Entity<Appointment>().Property(a => a.Title).HasMaxLength(FieldLengths.Title);
            modelBuilder.Entity<Appointment>().Property(a => a.Reason).HasMaxLength(FieldLengths.Reason);
            modelBuilder.Entity<Appointment>().Property(a => a.Area).HasMaxLength(FieldLengths.Area);
            modelBuilder.Entity<Appointment>().Property(a => a.Location).HasMaxLength(FieldLengths.Location);
            modelBuilder.Entity<Appointment>().Property(a => a.Notes).HasMaxLength(FieldLengths.LongText);

            modelBuilder.Entity<Document>().Property(d => d.FileName).HasMaxLength(FieldLengths.FileName);
            modelBuilder.Entity<Document>().Property(d => d.FilePath).HasMaxLength(FieldLengths.FilePath);
            modelBuilder.Entity<Document>().Property(d => d.ContentType).HasMaxLength(FieldLengths.ContentType);

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
                .HasOne<Client>()
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

            // A lawyer cannot have two appointments in the same slot (requirements: schedule
            // conflict). The in-memory HasScheduleConflict check returns the nicer error, but
            // two simultaneous requests can both pass it; this index is what actually closes
            // the race. Filter: a canceled appointment (Status 2) frees the slot. "Finalizado"
            // is never persisted (EffectiveStatus is computed), so the database only holds 0, 1 and 2.
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.LawyerId, a.Date, a.Time })
                .IsUnique()
                .HasDatabaseName("UX_Appointments_Lawyer_Slot")
                .HasFilter("[Active] = 1 AND [Status] <> 2");

            modelBuilder.Entity<Appointment>()
                .HasOne<Client>()
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne<Lawyer>()
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
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}