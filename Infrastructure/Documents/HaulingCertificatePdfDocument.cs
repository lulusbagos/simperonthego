using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Content().Column(col =>
            {
                col.Spacing(10);

                col.Item().Border(1).BorderColor(Colors.Blue.Lighten2).Padding(14).Column(header =>
                {
                    header.Spacing(5);
                    header.Item().Row(row =>
                    {
                        row.ConstantItem(72).Height(52).AlignMiddle().AlignLeft().Element(x =>
                        {
                            if (!string.IsNullOrWhiteSpace(_model.CompanyLogoPath) && File.Exists(_model.CompanyLogoPath))
                            {
                                x.Image(_model.CompanyLogoPath!, ImageScaling.FitArea);
                                return;
                            }

                            x.Text("INDEXIM").Bold().FontColor(Colors.Blue.Darken2);
                        });

                        row.RelativeItem().Column(mid =>
                        {
                            mid.Spacing(3);
                            mid.Item().AlignCenter().Text("SERTIFIKAT KEPATUHAN KETENTUAN JALAN HAULING")
                                .FontSize(15).Bold().FontColor(Colors.Blue.Darken2);
                            mid.Item().AlignCenter().Text("PT Indexim Coalindo").FontSize(11).SemiBold();
                            mid.Item().AlignCenter().Text($"Certificate ID: {_model.CertificateId ?? "-"} | Req ID: {_model.ReqId}").FontSize(8.5f);
                            mid.Item().AlignCenter().Text($"Tanggal Terbit: {(_model.CertifiedAt ?? DateTimeOffset.UtcNow).ToLocalTime():dd MMM yyyy HH:mm} WITA").FontSize(8.5f);
                        });

                        row.ConstantItem(72).Height(52).AlignMiddle().AlignRight().Element(x =>
                        {
                            if (!string.IsNullOrWhiteSpace(_model.AppLogoPath) && File.Exists(_model.AppLogoPath))
                            {
                                x.Image(_model.AppLogoPath!, ImageScaling.FitArea);
                                return;
                            }

                            x.Text("SIMPER").Bold().FontColor(Colors.Blue.Darken2);
                        });
                    });
                });

                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(left =>
                    {
                        left.Spacing(4);
                        left.Item().Text("Data Karyawan").Bold().FontSize(11);
                        left.Item().Text($"Nama: {ValueOrDash(permit.Nama)}");
                        left.Item().Text($"NIK: {ValueOrDash(permit.Nik)}");
                        left.Item().Text($"Perusahaan: {ValueOrDash(permit.Perusahaan)}");
                        left.Item().Text($"Departemen: {ValueOrDash(permit.Departemen)}");
                        left.Item().Text($"Jabatan: {ValueOrDash(permit.Jabatan)}");
                        left.Item().Text($"Posisi: {ValueOrDash(permit.Posisi)}");
                        left.Item().Text($"Akses Lokasi: {ValueOrDash(permit.AksesLokasi)}");
                    });

                    row.ConstantItem(160).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(right =>
                    {
                        right.Spacing(6);
                        right.Item().AlignCenter().Text("QR Verifikasi").Bold();
                        right.Item().AlignCenter().Image(BuildQrPng(_model.CertificateUrl ?? string.Empty));
                        right.Item().AlignCenter().Text("Scan untuk verifikasi publik").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().Border(1).BorderColor(Colors.Green.Lighten2).Background(Colors.Green.Lighten5).Padding(12).Text(
                    "Pemegang sertifikat menyatakan bersedia mematuhi dan bertanggung jawab penuh atas segala ketentuan jalan hauling PT Indexim Coalindo, termasuk konsekuensi operasional dan disipliner apabila terjadi pelanggaran terhadap prosedur keselamatan yang berlaku.");

                col.Item().Text($"URL Verifikasi: {_model.CertificateUrl ?? "-"}").FontSize(8).FontColor(Colors.Blue.Darken2);
            });
        });
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static byte[] BuildQrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(12);
    }
}
