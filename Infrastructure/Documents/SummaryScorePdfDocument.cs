using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimperSecureOnlineTestSystem.Domain.Enums;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Infrastructure.Documents;

public class SummaryScorePdfDocument : IDocument
{
    private readonly SummaryScoreViewModel _model;

    public SummaryScorePdfDocument(SummaryScoreViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var shouldMovePracticalBreakdownToNextPage = _model.PracticalItems.Count >= 5;

        container.Page(page =>
        {
            page.Margin(28);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1D2732"));
            page.Background().Element(ComposeWatermark);

            page.Content().Column(column =>
            {
                column.Spacing(14);
                column.Item().ShowEntire().Element(ComposeHeader);
                column.Item().ShowEntire().Element(ComposeHero);
                column.Item().ShowEntire().Row(row =>
                {
                    row.RelativeItem().Element(ComposeEmployeeCard);
                    row.ConstantItem(158).Element(ComposeQrCard);
                });
                column.Item().ShowEntire().Element(ComposeScoreCards);
                if (shouldMovePracticalBreakdownToNextPage)
                {
                    column.Item().PageBreak();
                }
                column.Item().Element(ComposePracticalTable);
                column.Item().ShowEntire().Element(ComposeFooter);
            });
        });
    }

    private void ComposeWatermark(IContainer container)
    {
        container
            .AlignCenter()
            .AlignMiddle()
            .Rotate(-32)
            .Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(46).SemiBold().FontColor("#DCE6EE"));
                text.Span("OFFICIAL DOCUMENT");
                text.EmptyLine();
                text.DefaultTextStyle(x => x.FontSize(15).SemiBold().FontColor("#E5EDF3"));
                text.Span(_model.SummaryId);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .PaddingVertical(16)
            .PaddingHorizontal(18)
            .Row(row =>
            {
                row.ConstantItem(108).Height(62).Element(ComposeCompanyLogo);
                row.RelativeItem().PaddingHorizontal(10).AlignCenter().Column(column =>
                {
                    column.Item().Text(_model.CompanyName)
                        .SemiBold()
                        .FontSize(13)
                        .FontColor("#20384D")
                        .AlignCenter();
                    column.Item().Text("OFFICIAL SIMPER COMPETENCY SUMMARY")
                        .Bold()
                        .FontSize(16)
                        .LetterSpacing(0.2f)
                        .FontColor("#2F4F6A")
                        .AlignCenter();
                    column.Item().Text("SIMPER On The Go Assessment Record")
                        .FontSize(9)
                        .FontColor("#617080")
                        .AlignCenter();
                    column.Item().Text($"Summary ID: {_model.SummaryId}")
                        .FontSize(9)
                        .FontColor("#617080")
                        .AlignCenter();
                });
                row.ConstantItem(88).Height(62).Element(ComposeAppLogo);
            });
    }

    private void ComposeHero(IContainer container)
    {
        container
            .Background(Colors.White)
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(18)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("Assessment Completion Summary")
                    .Bold()
                    .FontSize(14)
                    .FontColor("#20384D");
                column.Item().Text(
                    "Dokumen resmi ini merangkum hasil teori dan praktek peserta dalam format premium yang siap cetak atau diunduh.")
                    .FontSize(10)
                    .FontColor("#617080");
            });
    }

    private void ComposeEmployeeCard(IContainer container)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(16)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Employee Detail").Bold().FontSize(13).FontColor("#20384D");
                column.Item().Element(c => ComposeKeyValueGrid(c, new[]
                {
                    ("Nama", _model.EmployeeName),
                    ("NIK / NRP", _model.Nrp),
                    ("KTP", string.IsNullOrWhiteSpace(_model.Ktp) ? "-" : _model.Ktp),
                    ("Departemen", string.IsNullOrWhiteSpace(_model.DepartmentName) ? "-" : _model.DepartmentName),
                    ("Pengajuan", string.IsNullOrWhiteSpace(_model.SubmissionType) ? "-" : _model.SubmissionType),
                    ("Nomor Pengajuan", string.IsNullOrWhiteSpace(_model.SubmissionNumber) ? "-" : _model.SubmissionNumber),
                    ("Unit", _model.VehicleName),
                    ("Instruktur", string.IsNullOrWhiteSpace(_model.InstructorName) ? "-" : _model.InstructorName)
                }));
            });
    }

    private void ComposeQrCard(IContainer container)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(14)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("Proof of Test ID").Bold().FontSize(12).FontColor("#20384D").AlignCenter();
                column.Item().AlignCenter().Width(108).Height(108).Image(GenerateQrBytes(_model.QrTargetUrl));
                column.Item().Text(_model.QrProofText)
                    .FontSize(8)
                    .FontColor("#617080")
                    .AlignCenter();
                column.Item().Text("Scan QR untuk verifikasi summary publik")
                    .FontSize(6.5f)
                    .FontColor("#617080")
                    .AlignCenter();
            });
    }

    private void ComposeScoreCards(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(card => ComposeScoreCard(
                card,
                "Tes Teori",
                _model.TheoryPassed ? "LULUS" : "TIDAK LULUS",
                _model.TheoryScore.ToString("0.##"),
                $"Benar {_model.TheoryCorrectAnswers}/{_model.TheoryTotalQuestions}",
                _model.TheoryScheduledAt,
                _model.TheoryFinishedAt));

            row.ConstantItem(12);

            row.RelativeItem().Element(card => ComposeScoreCard(
                card,
                "Tes Praktek",
                _model.PracticalPassed ? "LULUS" : "TIDAK LULUS",
                _model.PracticalDisplayScore,
                _model.PracticalTemplateName,
                _model.PracticalScheduledAt,
                _model.PracticalFinishedAt));
        });
    }

    private void ComposeScoreCard(IContainer container, string title, string status, string score, string meta, DateTime scheduledAt, DateTime? completedAt)
    {
        var completedText = completedAt.HasValue
            ? completedAt.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm")
            : "-";

        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(16)
            .Column(column =>
            {
                column.Spacing(5);
                column.Item().Text(title).Bold().FontSize(13).FontColor("#20384D");
                column.Item().Text(status)
                    .Bold()
                    .FontSize(16)
                    .FontColor(status == "LULUS" ? "#1B6F5A" : "#B66774");
                column.Item().Text($"Nilai: {score}")
                    .SemiBold()
                    .FontSize(12)
                    .FontColor("#20384D");
                column.Item().Text(meta).FontSize(10).FontColor("#617080");
                column.Item().Text($"Jadwal: {scheduledAt.ToLocalTime():dd MMM yyyy HH:mm}")
                    .FontSize(9)
                    .FontColor("#617080");
                column.Item().Text($"Selesai: {completedText}")
                    .FontSize(9)
                    .FontColor("#617080");
            });
    }

    private void ComposePracticalTable(IContainer container)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(14)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Practical Assessment Breakdown").Bold().FontSize(13).FontColor("#20384D");
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(2.4f);
                        columns.ConstantColumn(46);
                        columns.ConstantColumn(46);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header.Cell(), "Section");
                        HeaderCell(header.Cell(), "Item");
                        HeaderCell(header.Cell(), "Bobot");
                        HeaderCell(header.Cell(), "Nilai");
                        HeaderCell(header.Cell(), "Catatan");
                    });

                    foreach (var item in _model.PracticalItems)
                    {
                        BodyCell(table.Cell(), item.SectionName);
                        BodyCell(table.Cell(), item.ItemLabel);
                        BodyCell(table.Cell(), item.Weight.ToString("0.##"));
                        BodyCell(table.Cell(), string.Equals(_model.PracticalScoringMode, PracticalScoringMode.Numeric, StringComparison.OrdinalIgnoreCase)
                            ? (item.NumericValue?.ToString("0.##") ?? "-")
                            : (item.GradeValue ?? "-"));
                        BodyCell(table.Cell(), string.IsNullOrWhiteSpace(item.Note) ? "-" : item.Note);
                    }
                });

                if (!string.IsNullOrWhiteSpace(_model.InstructorNote))
                {
                    column.Item().PaddingTop(6).Text($"Catatan instruktur: {_model.InstructorNote}")
                        .FontSize(9)
                        .FontColor("#617080");
                }
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor("#D7DFE8")
            .PaddingTop(10)
            .Row(row =>
            {
                row.RelativeItem().Text("Dokumen resmi SIMPER On The Go. Verifikasi melalui QR Code atau halaman publik aplikasi.")
                    .FontSize(9)
                    .FontColor("#617080");
                row.ConstantItem(140).AlignRight().Text(text =>
                {
                    text.Span("Status akhir: ").SemiBold();
                    text.Span(_model.FinalStatus).Bold().FontColor(_model.FinalStatus == "LULUS" ? "#1B6F5A" : "#B66774");
                });
            });
    }

    private void ComposeCompanyLogo(IContainer container)
    {
        if (File.Exists(_model.CompanyLogoPath))
        {
            container.Image(File.ReadAllBytes(_model.CompanyLogoPath)).FitArea();
            return;
        }

        container
            .Border(1)
            .BorderColor("#D7DFE8")
            .Background("#F3F6FA")
            .AlignCenter()
            .AlignMiddle()
            .Text("INDEXIM")
            .Bold()
            .FontSize(16)
            .FontColor("#20384D");
    }

    private void ComposeAppLogo(IContainer container)
    {
        if (File.Exists(_model.AppLogoPath))
        {
            container.Image(File.ReadAllBytes(_model.AppLogoPath)).FitArea();
            return;
        }

        container
            .Border(1)
            .BorderColor("#D7DFE8")
            .Background("#F3F6FA")
            .AlignCenter()
            .AlignMiddle()
            .Text("SIMPER")
            .Bold()
            .FontSize(15)
            .FontColor("#2F4F6A");
    }

    private static void ComposeKeyValueGrid(IContainer container, IEnumerable<(string Label, string Value)> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(110);
                columns.RelativeColumn();
            });

            foreach (var item in items)
            {
                table.Cell().PaddingVertical(3).Text(item.Label).SemiBold().FontSize(9).FontColor("#617080");
                table.Cell().PaddingVertical(3).Text(item.Value).FontSize(9);
            }
        });
    }

    private static void HeaderCell(IContainer container, string value)
    {
        container.Background("#EDF2F7").BorderBottom(1).BorderColor("#D7DFE8").PaddingVertical(5).PaddingHorizontal(6)
            .Text(value).SemiBold().FontSize(8).FontColor("#20384D");
    }

    private static void BodyCell(IContainer container, string value)
    {
        container.BorderBottom(1).BorderColor("#E6ECF3").PaddingVertical(4).PaddingHorizontal(6)
            .Text(value).FontSize(8).FontColor("#1D2732");
    }

    private static byte[] GenerateQrBytes(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(8);
    }
}
