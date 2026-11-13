using System.Text;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
using LegalManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LegalManager.Infrastructure.Persistence
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, bool forceReset = false)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LegalManagerDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<LegalManagerDbContext>>();

            // Verificamos si los usuarios de la seed de prueba ya existen en la base de datos
            var hasDemoLawyer = await context.Set<Lawyer>().AnyAsync(l => l.Email == "abogado@legalmanager.local");
            var hasDemoClient = await context.Set<Client>().AnyAsync(c => c.Email == "cliente@legalmanager.local");

            // Si falta alguno de los roles de prueba (como ocurre al desplegar sobre una base con datos viejos),
            // o si se solicitó explícitamente, forzamos el reseteo limpio completo.
            if (!hasDemoLawyer || !hasDemoClient)
            {
                logger.LogInformation("No se detectaron los usuarios de la seed de prueba ('abogado@legalmanager.local' o 'cliente@legalmanager.local'). Forzando reinicialización limpia...");
                forceReset = true;
            }
            else if (!forceReset)
            {
                logger.LogInformation("La base de datos ya contiene los usuarios de la seed de prueba requeridos, se omite el seed.");
                return;
            }

            if (forceReset)
            {
                logger.LogInformation("Vaciando base de datos para reinicialización limpia...");
                await context.Database.ExecuteSqlRawAsync(@"
                    SET QUOTED_IDENTIFIER ON;
                    SET ANSI_NULLS ON;
                    DELETE FROM Documents;
                    DELETE FROM Appointments;
                    DELETE FROM CaseLawyer;
                    DELETE FROM Cases;
                    DELETE FROM Clients;
                    DELETE FROM Lawyers;
                    DELETE FROM Admins;");
            }

            logger.LogInformation("Iniciando seed de datos de prueba con roles Admin, Abogado y Cliente...");

            // 1. Usuarios con roles (Admin, Abogado, Cliente)
            var admin = new Admin(
                "Admin",
                "Sistema",
                "00000000",
                "admin@legalmanager.local",
                passwordHasher.Hash("Admin123!"));
            admin.ConfirmEmail();
            context.Set<Admin>().Add(admin);

            var lawyer = new Lawyer(
                "Franco",
                "Cuscianna",
                "30111222",
                "abogado@legalmanager.local",
                passwordHasher.Hash("Abogado123!"),
                "T° 50 F° 123",
                "341-555-1234",
                ["Civil", "Familia", "Comercial"],
                "{\"days\":[\"Lunes\",\"Martes\",\"Miércoles\",\"Jueves\",\"Viernes\"],\"timeSlots\":[\"09:00\",\"10:00\",\"11:00\",\"12:00\",\"13:00\",\"14:00\",\"15:00\",\"16:00\",\"17:00\"]}");
            lawyer.ConfirmEmail();
            context.Set<Lawyer>().Add(lawyer);

            var client = new Client(
                "Agustín",
                "Angelini",
                "40999888",
                "cliente@legalmanager.local",
                passwordHasher.Hash("Cliente123!"),
                "341-555-8899",
                "Bv. Oroño 1234, Rosario");
            client.ConfirmEmail();
            context.Set<Client>().Add(client);

            await context.SaveChangesAsync();

            // 2. Expedientes (Cases)
            // Caso 1: Activo
            var case1 = new Case(
                "EXP-2026-001",
                "Divorcio vincular de común acuerdo",
                "Familia",
                DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)),
                "Trámite de divorcio vincular presentado de común acuerdo con propuesta reguladora de bienes familiares.",
                "Audiencia fijada para ratificación y homologación judicial.",
                client.Id,
                lawyer,
                lawyer.Id);
            context.Cases.Add(case1);

            // Caso 2: Pendiente
            var case2 = new Case(
                "EXP-2026-002",
                "Sucesión testamentaria Gómez",
                "Civil",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-15)),
                "Apertura de juicio sucesorio testamentario, inventario y avalúo de bienes relictos.",
                "A la espera de informes de la Dirección Provincial del Registro General.",
                client.Id,
                lawyer,
                lawyer.Id);
            case2.ChangeStatus(CaseStatus.Pendiente);
            context.Cases.Add(case2);

            // Caso 3: Cerrado
            var case3 = new Case(
                "EXP-2026-003",
                "Cobro ejecutivo de pagaré",
                "Comercial",
                DateOnly.FromDateTime(DateTime.Now.AddMonths(-3)),
                "Juicio ejecutivo por cobro de pagaré comercial con embargo preventivo trabado.",
                "Convenio de cancelación extrajudicial homologado e íntegramente cancelado.",
                client.Id,
                lawyer,
                admin.Id);
            case3.ChangeStatus(CaseStatus.Cerrado);
            context.Cases.Add(case3);

            await context.SaveChangesAsync();

            // 3. Documentos (PDFs reales en IFileStorage)
            var samplePdfBytes = Encoding.ASCII.GetBytes(
                "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" +
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n" +
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n" +
                "4 0 obj\n<< /Length 44 >>\nstream\nBT /F1 12 Tf 100 700 Td (Documento Legal) Tj ET\nendstream\nendobj\nxref\n0 5\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n0000000214 00000 n \ntrailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n308\n%%EOF\n"
            );

            // Doc 1 (Caso 1)
            var path1 = await fileStorage.SaveAsync(case1.Id, "Demanda_Inicial.pdf", new MemoryStream(samplePdfBytes), "application/pdf");
            var doc1 = new Document(case1.Id, "Demanda_Inicial.pdf", path1, "application/pdf", samplePdfBytes.Length, DocumentType.Escrito, lawyer.Id);
            context.Documents.Add(doc1);

            // Doc 2 (Caso 1)
            var path2 = await fileStorage.SaveAsync(case1.Id, "Convenio_Regulador.pdf", new MemoryStream(samplePdfBytes), "application/pdf");
            var doc2 = new Document(case1.Id, "Convenio_Regulador.pdf", path2, "application/pdf", samplePdfBytes.Length, DocumentType.Contrato, lawyer.Id);
            context.Documents.Add(doc2);

            // Doc 3 (Caso 2)
            var path3 = await fileStorage.SaveAsync(case2.Id, "Testamento_Olografo.pdf", new MemoryStream(samplePdfBytes), "application/pdf");
            var doc3 = new Document(case2.Id, "Testamento_Olografo.pdf", path3, "application/pdf", samplePdfBytes.Length, DocumentType.Prueba, lawyer.Id);
            context.Documents.Add(doc3);

            // Doc 4 (Caso 3: Resumen IA)
            var summaryText = "Expediente de cobro ejecutivo finalizado con éxito. El demandado saldó el total de la deuda según convenio.";
            var path4 = await fileStorage.SaveAsync(case3.Id, "ResumenIA.pdf", new MemoryStream(samplePdfBytes), "application/pdf");
            var doc4 = Document.CreateAiSummary(case3.Id, "ResumenIA.pdf", path4, summaryText, lawyer.Id);
            doc4.Approve(admin.Id);
            context.Documents.Add(doc4);

            // Doc 5 (En papelera para pruebas)
            var path5 = await fileStorage.SaveAsync(case1.Id, "Borrador_Antiguo.pdf", new MemoryStream(samplePdfBytes), "application/pdf");
            var doc5 = new Document(case1.Id, "Borrador_Antiguo.pdf", path5, "application/pdf", samplePdfBytes.Length, DocumentType.Escrito, lawyer.Id);
            doc5.Deactivate();
            context.Documents.Add(doc5);

            // 4. Turnos (Appointments)
            // Turno 1: Mañana (Pendiente)
            var app1 = new Appointment(
                "Solicitud de turno",
                DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                new TimeOnly(10, 0),
                new TimeOnly(11, 0),
                "Consulta inicial por sucesión",
                "Civil",
                "Oficina Central - Piso 2",
                "Reunión presencial para revisión de documentación hereditaria.",
                client.Id,
                lawyer.Id,
                case2.Id);
            context.Appointments.Add(app1);

            // Turno 2: Próxima semana (Confirmado)
            var app2 = new Appointment(
                "Firma de convenio regulador",
                DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                new TimeOnly(11, 0),
                new TimeOnly(12, 0),
                "Firma de convenio",
                "Familia",
                "Sala de Reuniones A",
                "Firma de escrito conjunto de divorcio.",
                client.Id,
                lawyer.Id,
                case1.Id);
            app2.Confirm();
            context.Appointments.Add(app2);

            // Turno 3: Pasado (Finalizado)
            var app3 = new Appointment(
                "Entrevista preliminar de divorcio",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-7)),
                new TimeOnly(15, 0),
                new TimeOnly(16, 0),
                "Consulta preliminar",
                "Familia",
                "Oficina 104",
                "Primera reunión informativa con el cliente.",
                client.Id,
                lawyer.Id,
                case1.Id);
            app3.Confirm();
            context.Appointments.Add(app3);

            // Turno 4: En papelera para pruebas
            var app4 = new Appointment(
                "Turno reprogramado anterior",
                DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                "Revisión documental",
                "Civil",
                "Despacho B",
                "Cancelado por cambio de fecha.",
                client.Id,
                lawyer.Id,
                case2.Id);
            app4.Deactivate();
            context.Appointments.Add(app4);

            await context.SaveChangesAsync();

            logger.LogInformation("Seed de datos ficticios completado exitosamente: 3 usuarios, 3 expedientes, 5 documentos y 4 turnos creados.");
        }
    }
}
