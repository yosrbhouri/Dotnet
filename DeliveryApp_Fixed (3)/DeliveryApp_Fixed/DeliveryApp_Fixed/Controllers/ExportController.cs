using ClosedXML.Excel;
using DeliveryApp.Data;
using DeliveryApp.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExportController : Controller
    {
        private readonly AppDbContext _context;

        // ── Palette de couleurs ──────────────────────────────────────────────
        private static readonly BaseColor CWhite = new BaseColor(255, 255, 255);
        private static readonly BaseColor CBlack = new BaseColor(0, 0, 0);
        private static readonly BaseColor CGray = new BaseColor(128, 128, 128);
        private static readonly BaseColor CDarkGray = new BaseColor(64, 64, 64);
        private static readonly BaseColor CNavy = new BaseColor(11, 29, 58);
        private static readonly BaseColor COrange = new BaseColor(255, 107, 53);
        private static readonly BaseColor CGreen = new BaseColor(40, 167, 69);
        private static readonly BaseColor CYellow = new BaseColor(255, 193, 7);
        private static readonly BaseColor CRowAlt = new BaseColor(245, 247, 250);
        private static readonly BaseColor CBorder = new BaseColor(220, 220, 220);

        public ExportController(AppDbContext context)
        {
            _context = context;
        }

        // ─── Page d'accueil des exports ──────────────────────────────────────
        public IActionResult Exports() => View("~/Views/Admin/Exports.cshtml");

        // ─── Export Colis PDF ────────────────────────────────────────────────
        public async Task<IActionResult> ColisPdf()
        {
            var colis = await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4.Rotate(), 20f, 20f, 30f, 20f);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // ── Table principale ──
            var table = new PdfPTable(8) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 5f, 18f, 18f, 15f, 10f, 10f, 12f, 12f });

            // Titre
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, CWhite);
            table.AddCell(new PdfPCell(new Phrase("Liste des Colis — DeliveryApp", titleFont))
            {
                BackgroundColor = CNavy,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 12f,
                Border = 0,
                Colspan = 8
            });

            // En-têtes
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, CWhite);
            foreach (var h in new[] { "#", "Description", "Client", "Livreur", "Montant", "Poids", "Statut", "Date" })
            {
                table.AddCell(new PdfPCell(new Phrase(h, headerFont))
                {
                    BackgroundColor = COrange,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 7f,
                    Border = 0
                });
            }

            // Lignes de données
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, CDarkGray);
            bool alt = false;
            foreach (var c in colis)
            {
                var bg = alt ? CRowAlt : CWhite;
                var statutText = c.Statut switch
                {
                    StatutColis.EnAttente => "En attente",
                    StatutColis.EnCours => "En cours",
                    StatutColis.Livré => "Livré",
                    _ => "Annulé"
                };
                var vals = new[]
                {
                    $"#{c.Id}",
                    c.Description ?? "—",
                    c.Client != null ? $"{c.Client.Nom} {c.Client.Prenom}" : "—",
                    c.Livreur?.RaisonSociale ?? "—",
                    $"{c.Montant:N2} DT",
                    $"{c.Poids} kg",
                    statutText,
                    c.DateLivraison.ToString("dd/MM/yyyy")
                };
                foreach (var val in vals)
                {
                    table.AddCell(new PdfPCell(new Phrase(val, cellFont))
                    {
                        BackgroundColor = bg,
                        Padding = 6f,
                        Border = 0,
                        BorderWidthBottom = 0.5f,
                        BorderColorBottom = CBorder
                    });
                }
                alt = !alt;
            }

            doc.Add(table);
            doc.Add(new Paragraph(
                $"\nGénéré le {DateTime.Now:dd/MM/yyyy à HH:mm}",
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, CGray)));
            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"colis_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ─── Export Colis Excel ──────────────────────────────────────────────
        public async Task<IActionResult> ColisExcel()
        {
            var colis = await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Colis");

            // Titre fusionné
            ws.Range("A1:H1").Merge();
            ws.Cell("A1").Value = "Liste des Colis — DeliveryApp";
            ws.Cell("A1").Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromArgb(11, 29, 58))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(1).Height = 30;

            // En-têtes
            var headers = new[] { "#", "Description", "Client", "Livreur", "Montant (DT)", "Poids (kg)", "Statut", "Date Livraison" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(255, 107, 53))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Row(2).Height = 20;

            // Données
            int row = 3;
            foreach (var c in colis)
            {
                var statutText = c.Statut switch
                {
                    StatutColis.EnAttente => "En attente",
                    StatutColis.EnCours => "En cours",
                    StatutColis.Livré => "Livré",
                    _ => "Annulé"
                };

                ws.Cell(row, 1).Value = $"#{c.Id}";
                ws.Cell(row, 2).Value = c.Description ?? "—";
                ws.Cell(row, 3).Value = c.Client != null ? $"{c.Client.Nom} {c.Client.Prenom}" : "—";
                ws.Cell(row, 4).Value = c.Livreur?.RaisonSociale ?? "—";
                ws.Cell(row, 5).Value = c.Montant;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Value = c.Poids;
                ws.Cell(row, 7).Value = statutText;
                ws.Cell(row, 8).Value = c.DateLivraison.ToString("dd/MM/yyyy");

                // Couleur statut
                var statutColor = c.Statut switch
                {
                    StatutColis.EnAttente => XLColor.FromArgb(255, 243, 205),
                    StatutColis.EnCours => XLColor.FromArgb(207, 226, 255),
                    StatutColis.Livré => XLColor.FromArgb(198, 239, 206),
                    _ => XLColor.FromArgb(255, 199, 206)
                };
                ws.Cell(row, 7).Style.Fill.SetBackgroundColor(statutColor);

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.SetBackgroundColor(XLColor.FromArgb(248, 249, 250));

                row++;
            }

            ws.RangeUsed()?.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"colis_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ─── Rapport mensuel PDF ─────────────────────────────────────────────
        public async Task<IActionResult> RapportMensuelPdf(int? mois, int? annee)
        {
            mois ??= DateTime.Now.Month;
            annee ??= DateTime.Now.Year;

            var colis = await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .Where(c => c.DateCreation.Month == mois && c.DateCreation.Year == annee)
                .ToListAsync();

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4, 40f, 40f, 50f, 40f);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Titre
            doc.Add(new Paragraph(
                $"Rapport Mensuel — {new DateTime(annee.Value, mois.Value, 1):MMMM yyyy}",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, CNavy))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 5f });

            doc.Add(new Paragraph(
                "DeliveryApp — Gestion des Livraisons",
                FontFactory.GetFont(FontFactory.HELVETICA, 10, CGray))
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20f });

            // ── Cartes statistiques ──
            var statsTable = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20f };
            var stats = new[]
            {
                ("Total Colis",        colis.Count.ToString(),                                  CNavy),
                ("Chiffre d'affaires", $"{colis.Sum(c => c.Montant):N2} DT",                   COrange),
                ("Livrés",             colis.Count(c => c.Statut == StatutColis.Livré).ToString(), CGreen),
                ("En attente",         colis.Count(c => c.Statut == StatutColis.EnAttente).ToString(), CYellow)
            };

            foreach (var (label, val, color) in stats)
            {
                var cell = new PdfPCell { Border = 0, Padding = 12f, BackgroundColor = color };
                cell.AddElement(new Paragraph(val,
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22, CWhite))
                { Alignment = Element.ALIGN_CENTER });
                cell.AddElement(new Paragraph(label,
                    FontFactory.GetFont(FontFactory.HELVETICA, 9, CWhite))
                { Alignment = Element.ALIGN_CENTER });
                statsTable.AddCell(cell);
            }
            doc.Add(statsTable);

            // ── Tableau détail colis ──
            if (colis.Any())
            {
                doc.Add(new Paragraph("Détail des colis",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, CNavy))
                { SpacingAfter = 8f });

                var table = new PdfPTable(6) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 5f, 20f, 20f, 15f, 12f, 13f });

                var hFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, CWhite);
                foreach (var h in new[] { "#", "Description", "Client", "Livreur", "Montant", "Statut" })
                {
                    table.AddCell(new PdfPCell(new Phrase(h, hFont))
                    {
                        BackgroundColor = COrange,
                        Padding = 6f,
                        Border = 0,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    });
                }

                var dFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, CDarkGray);
                bool alt = false;
                foreach (var c in colis)
                {
                    var bg = alt ? new BaseColor(248, 249, 250) : CWhite;
                    var vals = new[]
                    {
                        $"#{c.Id}",
                        c.Description ?? "—",
                        c.Client != null ? $"{c.Client.Nom} {c.Client.Prenom}" : "—",
                        c.Livreur?.RaisonSociale ?? "—",
                        $"{c.Montant:N2} DT",
                        c.Statut.ToString()
                    };
                    foreach (var v in vals)
                    {
                        table.AddCell(new PdfPCell(new Phrase(v, dFont))
                        {
                            BackgroundColor = bg,
                            Padding = 5f,
                            Border = 0,
                            BorderWidthBottom = 0.5f,
                            BorderColorBottom = CBorder
                        });
                    }
                    alt = !alt;
                }
                doc.Add(table);
            }

            doc.Add(new Paragraph(
                $"\nGénéré le {DateTime.Now:dd/MM/yyyy à HH:mm}",
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, CGray)));
            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"rapport_{mois:D2}_{annee}.pdf");
        }

        // ─── Chiffre d'affaires par Livreur Excel ───────────────────────────
        public async Task<IActionResult> CaLivreurExcel()
        {
            var livreurs = await _context.Livreurs
                .Include(l => l.Colis)
                .Include(l => l.Vehicules)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CA par Livreur");

            ws.Range("A1:F1").Merge();
            ws.Cell("A1").Value = "Chiffre d'Affaires par Livreur";
            ws.Cell("A1").Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromArgb(11, 29, 58))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(1).Height = 30;

            var headers = new[] { "Livreur", "Véhicule", "Total Colis", "Livrés", "CA Total (DT)", "CA Moyen (DT)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(2, i + 1).Value = headers[i];
                ws.Cell(2, i + 1).Style
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(255, 107, 53))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Row(2).Height = 20;

            int row = 3;
            foreach (var l in livreurs.OrderByDescending(l => l.Colis.Sum(c => c.Montant)))
            {
                ws.Cell(row, 1).Value = l.RaisonSociale;
                ws.Cell(row, 2).Value = l.Vehicules.Any() ? string.Join(", ", l.Vehicules.Select(v => v.Marque)) : "—";
                ws.Cell(row, 3).Value = l.Colis.Count;
                ws.Cell(row, 4).Value = l.Colis.Count(c => c.Statut == StatutColis.Livré);
                ws.Cell(row, 5).Value = l.Colis.Sum(c => c.Montant);
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Value = l.Colis.Any() ? l.Colis.Average(c => c.Montant) : 0;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.SetBackgroundColor(XLColor.FromArgb(248, 249, 250));
                row++;
            }

            ws.RangeUsed()?.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ca_livreurs_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}