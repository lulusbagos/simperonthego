using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.ViewModels;

namespace SimperSecureOnlineTestSystem.Infrastructure.Documents;

public class HaulingCertificatePdfDocument : IDocument
{
    private readonly PermitHaulingDetailViewModel _model;

    public HaulingCertificatePdfDocument(PermitHaulingDetailViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var permit = _model.Permit;
        if (permit is null)
        {
            throw new InvalidOperationException("Data permit tidak tersedia untuk pembuatan PDF.");
        }

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1D2732").FontFamily("Arial"));
            page.Background().Element(ComposeWatermark);

            page.Content().Column(col =>
            {
                col.Spacing(12);
                col.Item().ShowEntire().Element(ComposeHeader);
                col.Item().ShowEntire().Element(ComposeHero);
                col.Item().Element(c => ComposeEmployeeCard(c, permit));
                col.Item().Element(c => ComposeQrCard(c, _model.CertificateUrl ?? string.Empty));
                col.Item().ShowEntire().Element(ComposeComplianceStatement);
                col.Item().ShowEntire().Element(ComposeVerificationBox);
                col.Item().ShowEntire().Element(ComposeFooter);
            });
        });
    }

    private void ComposeWatermark(IContainer container)
    {
        container
            .AlignCenter()
            .AlignMiddle()
            .Rotate(-30)
            .Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(44).SemiBold().FontColor("#E5EDF5"));
                text.Span("HAULING COMPLIANCE");
                text.EmptyLine();
                text.DefaultTextStyle(x => x.FontSize(13).SemiBold().FontColor("#EDF2F8"));
                text.Span(_model.CertificateId ?? "CERTIFICATE");
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .Background(Colors.White)
            .Border(1)
            .BorderColor("#D7DFE8")
            .PaddingVertical(14)
            .PaddingHorizontal(16)
            .Row(row =>
            {
                row.ConstantItem(104).Height(62).Element(ComposeCompanyLogo);
                row.RelativeItem().PaddingHorizontal(8).AlignCenter().Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Text("PT INDEXIM COALINDO")
                        .SemiBold()
                        .FontSize(12)
                        .FontColor("#20384D")
                        .AlignCenter();
                    column.Item().Text("SERTIFIKAT KEPATUHAN KETENTUAN JALAN HAULING")
                        .Bold()
                        .FontSize(15)
                        .LetterSpacing(0.15f)
                        .FontColor("#254E73")
                        .AlignCenter();
                    column.Item().Text("SIMPER On The Go - Compliance Record")
                        .FontSize(8.5f)
                        .FontColor("#617080")
                        .AlignCenter();
                });
                row.ConstantItem(88).Height(62).Element(ComposeAppLogo);
            });
    }

    private void ComposeHero(IContainer container)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(14)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("Official Completion Statement")
                    .Bold()
                    .FontSize(12.5f)
                    .FontColor("#20384D");
                column.Item().Text(
                    "Dokumen ini menyatakan bahwa peserta telah menyelesaikan pembacaan ketentuan jalan hauling, memahami poin keselamatan operasional, dan memberikan persetujuan resmi pada sistem.")
                    .FontSize(9.8f)
                    .FontColor("#4B5E71");
            });
    }

    private void ComposeEmployeeCard(IContainer container, PermitApprovalView permit)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(14)
            .Column(column =>
            {
                column.Spacing(7);
                column.Item().Text("Data Karyawan dan Permit")
                    .Bold()
                    .FontSize(12)
                    .FontColor("#20384D");
                column.Item().Element(c => ComposeKeyValueGrid(c, new[]
                {
                    ("Certificate ID", _model.CertificateId ?? "-"),
                    ("Req ID", _model.ReqId),
                    ("Nomor Permit", ValueOrDash(permit.Nomor)),
                    ("Tanggal Permit", permit.Tanggal.HasValue ? permit.Tanggal.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm") : "-"),
                    ("NIK", ValueOrDash(permit.Nik)),
                    ("Nama", ValueOrDash(permit.Nama)),
                    ("Perusahaan", ValueOrDash(permit.Perusahaan)),
                    ("Departemen", ValueOrDash(permit.Departemen)),
                    ("Jabatan", ValueOrDash(permit.Jabatan)),
                    ("Posisi", ValueOrDash(permit.Posisi)),
                    ("Akses Lokasi", ValueOrDash(permit.AksesLokasi)),
                    ("Status Kepatuhan", "KETENTUAN_HAULING = TRUE")
                }));
            });
    }

    private void ComposeQrCard(IContainer container, string url)
    {
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text("QR Verifikasi Publik")
                    .Bold()
                    .FontSize(11)
                    .FontColor("#20384D")
                    .AlignCenter();

                column.Item().Row(row =>
                {
                    row.ConstantItem(150).AlignMiddle().AlignCenter().Column(qr =>
                    {
                        qr.Item().AlignCenter().Width(118).Height(118).Image(BuildQrPng(url));
                        qr.Item().AlignCenter().Text("Scan untuk membuka halaman verifikasi sertifikat.")
                            .FontSize(7.5f)
                            .FontColor("#617080");
                    });

                    row.RelativeItem().PaddingLeft(10).Column(info =>
                    {
                        info.Spacing(4);
                        info.Item().Text("Status Verifikasi").SemiBold().FontColor("#2B4D70");
                        info.Item().Text("Ketentuan hauling telah tercatat dan tervalidasi pada sistem.")
                            .FontSize(9)
                            .FontColor("#4B5E71");
                        info.Item().Text($"URL: {url}")
                            .FontSize(8)
                            .FontColor("#1E4F7C");
                    });
                });
            });
    }

    private void ComposeComplianceStatement(IContainer container)
    {
        container
            .Background("#F1F9F5")
            .Border(1)
            .BorderColor("#C5E2D5")
            .Padding(12)
            .Text("Pemegang sertifikat menyatakan bersedia mematuhi dan bertanggung jawab penuh atas segala ketentuan jalan hauling PT Indexim Coalindo, termasuk konsekuensi operasional dan disipliner apabila terjadi pelanggaran terhadap prosedur keselamatan yang berlaku.")
            .FontSize(9.6f)
            .FontColor("#1B4E3D");
    }

    private void ComposeVerificationBox(IContainer container)
    {
        var issuedAt = (_model.CertifiedAt ?? DateTimeOffset.UtcNow).ToLocalTime();
        container
            .Background("#FFFFFF")
            .Border(1)
            .BorderColor("#D7DFE8")
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Text($"URL Verifikasi: {_model.CertificateUrl ?? "-"}")
                    .FontSize(8.4f)
                    .FontColor("#1E4F7C");
                column.Item().Text($"Issued At: {issuedAt:dd MMM yyyy HH:mm} WITA")
                    .FontSize(8.2f)
                    .FontColor("#617080");
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(6)
            .BorderTop(1)
            .BorderColor("#DDE5EE")
            .Row(row =>
            {
                row.RelativeItem().Text("SIMPER On The Go - Official Hauling Compliance Certificate")
                    .FontSize(8)
                    .FontColor("#617080");
                row.ConstantItem(120).AlignRight().Text($"Doc: {_model.CertificateId ?? "-"}")
                    .FontSize(8)
                    .FontColor("#617080");
            });
    }

    private void ComposeCompanyLogo(IContainer container)
    {
        if (!string.IsNullOrWhiteSpace(_model.CompanyLogoPath) && File.Exists(_model.CompanyLogoPath))
        {
            container.Image(File.ReadAllBytes(_model.CompanyLogoPath!));
            return;
        }

        container.AlignCenter().AlignMiddle().Text("INDEXIM").Bold().FontColor("#20384D");
    }

    private void ComposeAppLogo(IContainer container)
    {
        if (!string.IsNullOrWhiteSpace(_model.AppLogoPath) && File.Exists(_model.AppLogoPath))
        {
            container.Image(File.ReadAllBytes(_model.AppLogoPath!));
            return;
        }

        container.AlignCenter().AlignMiddle().Text("SIMPER").Bold().FontColor("#20384D");
    }

    private static void ComposeKeyValueGrid(IContainer container, IEnumerable<(string Key, string Value)> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(120);
                columns.RelativeColumn();
            });

            foreach (var (key, value) in items)
            {
                table.Cell().PaddingVertical(2).Text(key).FontSize(9).FontColor("#617080");
                table.Cell().PaddingVertical(2).Text(value).SemiBold().FontSize(9.4f).FontColor("#20384D");
            }
        });
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static byte[] BuildQrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(string.IsNullOrWhiteSpace(text) ? "-" : text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(12);
    }
}
