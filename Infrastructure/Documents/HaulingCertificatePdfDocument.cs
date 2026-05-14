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
        var permit = _model.Permit ?? throw new InvalidOperationException("Data permit tidak tersedia.");
        var issuedAt = (_model.CertifiedAt ?? DateTimeOffset.UtcNow).ToLocalTime();

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1D2732"));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(10);

                col.Item().Border(1).BorderColor("#D8E2ED").Background("#FFFFFF").Padding(12).Column(x =>
                {
                    x.Spacing(6);
                    x.Item().Text("SERTIFIKAT KEPATUHAN KETENTUAN JALAN HAULING")
                        .FontSize(15)
                        .Bold()
                        .FontColor("#234A6D");
                    x.Item().Text("Dokumen resmi ini menyatakan peserta telah membaca, memahami, dan menyetujui ketentuan jalan hauling PT Indexim Coalindo.")
                        .FontSize(10)
                        .FontColor("#4E6072");
                    x.Item().Text($"Certificate ID: {_model.CertificateId ?? "-"}   |   Req ID: {_model.ReqId}")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor("#2F4F6A");
                    x.Item().Text($"Tanggal Terbit: {issuedAt:dd MMM yyyy HH:mm} WITA")
                        .FontSize(9)
                        .FontColor("#5B6E82");
                });

                col.Item().Border(1).BorderColor("#D8E2ED").Background("#FFFFFF").Padding(12).Column(x =>
                {
                    x.Spacing(4);
                    x.Item().Text("Data Karyawan dan Permit").Bold().FontSize(11).FontColor("#20384D");
                    x.Item().Text($"Nama: {ValueOrDash(permit.Nama)}");
                    x.Item().Text($"NIK: {ValueOrDash(permit.Nik)}");
                    x.Item().Text($"Perusahaan: {ValueOrDash(permit.Perusahaan)}");
                    x.Item().Text($"Departemen: {ValueOrDash(permit.Departemen)}");
                    x.Item().Text($"Jabatan: {ValueOrDash(permit.Jabatan)}");
                    x.Item().Text($"Posisi: {ValueOrDash(permit.Posisi)}");
                    x.Item().Text($"Akses Lokasi: {ValueOrDash(permit.AksesLokasi)}");
                    x.Item().Text($"Nomor Permit: {ValueOrDash(permit.Nomor)}");
                    x.Item().Text($"Status Kepatuhan: KETENTUAN_HAULING = TRUE").SemiBold().FontColor("#1B6F5A");
                });

                col.Item().Border(1).BorderColor("#D8E2ED").Background("#FFFFFF").Padding(12).Column(x =>
                {
                    x.Spacing(6);
                    x.Item().Text("QR Verifikasi Publik").Bold().FontSize(11).FontColor("#20384D").AlignCenter();
                    x.Item().AlignCenter().Width(120).Height(120).Image(BuildQrPng(_model.CertificateUrl ?? "-"));
                    x.Item().Text("Scan QR untuk verifikasi halaman sertifikat.").FontSize(8).FontColor("#5B6E82").AlignCenter();
                });

                col.Item().Border(1).BorderColor("#CBE4D7").Background("#F1F9F5").Padding(12).Text(
                    "Pemegang sertifikat menyatakan bersedia mematuhi dan bertanggung jawab penuh atas segala ketentuan jalan hauling PT Indexim Coalindo, termasuk konsekuensi operasional dan disipliner apabila terjadi pelanggaran terhadap prosedur keselamatan yang berlaku.")
                    .FontSize(9.5f)
                    .FontColor("#1B4E3D");

                col.Item().Border(1).BorderColor("#D8E2ED").Background("#FFFFFF").Padding(10).Column(x =>
                {
                    x.Spacing(2);
                    x.Item().Text($"URL Verifikasi: {_model.CertificateUrl ?? "-"}").FontSize(8.2f).FontColor("#1E4F7C");
                    x.Item().Text("SIMPER On The Go - Official Hauling Compliance Certificate").FontSize(8).FontColor("#617080");
                });
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .BorderBottom(1)
            .BorderColor("#DDE6EF")
            .PaddingBottom(8)
            .Row(row =>
            {
                row.ConstantItem(90).Height(52).Element(ComposeCompanyLogo);
                row.RelativeItem().AlignCenter().AlignMiddle().Text("PT INDEXIM COALINDO").SemiBold().FontSize(11).FontColor("#20384D");
                row.ConstantItem(70).Height(52).Element(ComposeAppLogo);
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

    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static byte[] BuildQrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(string.IsNullOrWhiteSpace(text) ? "-" : text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(12);
    }
}

