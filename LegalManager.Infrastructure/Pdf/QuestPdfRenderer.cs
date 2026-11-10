using LegalManager.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LegalManager.Infrastructure.Pdf
{
    // QuestPDF renderer for every PDF the system generates: the AI summary and
    // the fixed-template constancia. Segoe UI is used as the default font
    // because it ships with Windows (the App Service host).
    public class QuestPdfRenderer : IPdfDocumentRenderer
    {
        private const string FontFamily = "Segoe UI";
        private const string AccentColor = "#2F6FEB";
        private const string BodyColor = "#333333";
        private const string FooterColor = "#666666";

        static QuestPdfRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] RenderSummaryPdf(SummaryPdfData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontColor(BodyColor));

                    BuildHeader(page, "Resumen IA");

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Item().Text("Resumen del expediente").FontSize(18).Bold().FontColor(AccentColor);
                        column.Item().PaddingTop(14).Table(table =>
                        {
                            BuildCaseColumns(table);

                            AddCaseRow(table, "Número", data.CaseNumber);
                            AddCaseRow(table, "Título", data.Title);
                            AddCaseRow(table, "Área", data.Area);
                            AddCaseRow(table, "Estado", data.Status);
                            AddCaseRow(table, "Inicio", data.StartDate);
                            AddCaseRow(table, "Última actualización", data.LastUpdate);

                            // The closing date only exists once the case reaches Cerrado.
                            if (!string.IsNullOrWhiteSpace(data.ClosingDate))
                                AddCaseRow(table, "Cierre", data.ClosingDate);
                        });

                        BuildParagraphs(column.Item().PaddingTop(18), data.SummaryText);
                    });

                    page.Footer().AlignCenter().Text("Generado con IA a partir de los datos cargados en el sistema.").FontSize(8).FontColor(FooterColor);
                });
            }).GeneratePdf();
        }

        public byte[] RenderConstanciaPdf(ConstanciaPdfData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontColor(BodyColor));

                    BuildHeader(page, "Constancia");

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Item().Text("Constancia de expediente").FontSize(18).Bold().FontColor(AccentColor);
                        column.Item().PaddingTop(10).Text("Se expide la presente constancia del siguiente expediente:").FontSize(10);

                        column.Item().PaddingTop(14).Table(table =>
                        {
                            BuildCaseColumns(table);

                            AddCaseRow(table, "Número", data.CaseNumber);
                            AddCaseRow(table, "Título", data.Title);
                            AddCaseRow(table, "Cliente", data.ClientName);
                            AddCaseRow(table, "Abogados", data.LawyerNames);
                            AddCaseRow(table, "Área", data.Area);
                            AddCaseRow(table, "Estado", data.Status);
                            AddCaseRow(table, "Inicio", data.StartDate);
                            AddCaseRow(table, "Última actualización", data.LastUpdate);

                            // Full-width row: the description can wrap over
                            // several lines, so it spans both table columns.
                            table.Cell().ColumnSpan(2).PaddingTop(4).Text("Descripción").FontSize(10).SemiBold().FontColor(AccentColor);
                            table.Cell().ColumnSpan(2).Text(string.IsNullOrWhiteSpace(data.Description) ? "-" : data.Description).FontSize(10);
                        });
                    });

                    page.Footer().AlignCenter().Text($"Emitido el {data.GeneratedAt} por Legal Manager.").FontSize(8).FontColor(FooterColor);
                });
            }).GeneratePdf();
        }

        private static void BuildHeader(PageDescriptor page, string documentKind)
        {
            page.Header().Row(row =>
            {
                row.RelativeItem().Text("Legal Manager").FontSize(14).Bold().FontColor(AccentColor);
                row.RelativeItem().AlignRight().Text(documentKind).FontSize(10);
            });
        }

        // Two-column case fields table: fixed label column, wider value column
        // so long titles and names wrap instead of squeezing the label.
        private static void BuildCaseColumns(TableDescriptor table)
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
            });
        }

        private static void AddCaseRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Text(label).FontSize(10).SemiBold().FontColor(AccentColor);
            table.Cell().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(10);
        }

        // The AI summary is plain prose; blank lines separate its paragraphs.
        // When there is none, the whole text falls back to a single paragraph.
        private static void BuildParagraphs(IContainer container, string summaryText)
        {
            var paragraphs = summaryText
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToArray();

            if (paragraphs.Length == 0)
                paragraphs = new[] { summaryText.Trim() };

            container.Column(column =>
            {
                foreach (var paragraph in paragraphs)
                {
                    column.Item().PaddingBottom(6).Text(paragraph).FontSize(10).LineHeight(1.4f);
                }
            });
        }
    }
}
