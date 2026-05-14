using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using QRCoder;
using QuestPDF.Fluent;
using SimperSecureOnlineTestSystem.Domain.Entities;
using SimperSecureOnlineTestSystem.Infrastructure.Documents;
using SimperSecureOnlineTestSystem.ViewModels;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace SimperSecureOnlineTestSystem.Controllers;

[Route("ketentuanjalanhauling")]
public class PermitController : Controller
{
    private readonly IConfiguration _configuration;

    public PermitController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("")]
    public IActionResult Index(string? reqId)
    {
        if (!string.IsNullOrWhiteSpace(reqId))
        {
            return RedirectToAction(nameof(Detail), new { reqId = reqId.Trim() });
        }

        return View(new PermitHaulingLookupViewModel());
    }

    [HttpPost("search")]
    [ValidateAntiForgeryToken]
    public IActionResult Search(PermitHaulingLookupViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ReqId))
        {
            model.ErrorMessage = "Req ID wajib diisi.";
            return View("Index", model);
        }

        return RedirectToAction(nameof(Detail), new { reqId = model.ReqId.Trim() });
    }

    [HttpGet("{reqId}")]
    public async Task<IActionResult> Detail(string reqId, CancellationToken cancellationToken)
    {
        var vm = new PermitHaulingDetailViewModel
        {
            ReqId = reqId.Trim()
        };

        try
        {
            var lookup = await LoadPermitFromBimaAsync(vm.ReqId, cancellationToken);
            if (lookup is not null)
            {
                vm.Permit = lookup.Permit;
                vm.IsHaulingCompleted = lookup.IsHaulingCompleted;
                vm.CertifiedAt = DateTimeOffset.UtcNow;
                vm.CertificateId = BuildCertificateId(vm.ReqId);
                vm.CertificateUrl = BuildHaulingCertificateUrl(vm.ReqId);
                vm.CertificateQrDataUrl = BuildQrCodeDataUrl(vm.CertificateUrl);
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            vm.SourceUnavailable = true;
            vm.ErrorMessage = "Sumber data permit belum tersedia. Tabel public.tb_simper atau public.tb_permit tidak ditemukan.";
        }
        catch (RetryLimitExceededException)
        {
            vm.SourceUnavailable = true;
            vm.ErrorMessage = "Sumber data permit approval sedang tidak dapat diakses.";
        }
        catch (NpgsqlException)
        {
            vm.SourceUnavailable = true;
            vm.ErrorMessage = "Koneksi database Bima gagal dijangkau. Silakan coba kembali beberapa saat lagi.";
        }
        catch (DbUpdateException)
        {
            vm.SourceUnavailable = true;
            vm.ErrorMessage = "Data permit approval tidak dapat dimuat dari database saat ini.";
        }
        catch (InvalidOperationException ex)
        {
            vm.SourceUnavailable = true;
            vm.ErrorMessage = ex.Message.Contains("permit_id", StringComparison.OrdinalIgnoreCase)
                ? "Kolom permit_id pada tb_simper tidak ditemukan, sehingga detail permit belum bisa di-join."
                : ex.Message;
        }

        if (!vm.SourceUnavailable && vm.Permit is null)
        {
            vm.ErrorMessage = "Data permit approval dengan Req ID tersebut tidak ditemukan.";
        }

        return View(vm);
    }

    [HttpPost("{reqId}/submit")]
    public async Task<IActionResult> SubmitAgreement(string reqId, CancellationToken cancellationToken)
    {
        var normalizedReqId = reqId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReqId))
        {
            return BadRequest(new { success = false, message = "Req ID tidak valid." });
        }

        try
        {
            var updateResult = await MarkKetentuanHaulingCompletedAsync(normalizedReqId, cancellationToken);
            if (!updateResult.TargetFound)
            {
                return NotFound(new { success = false, message = "Req ID tidak ditemukan pada data SIMPER." });
            }

            var certificateUrl = BuildHaulingCertificateUrl(normalizedReqId);
            return Json(new
            {
                success = true,
                message = updateResult.Updated
                    ? "Persetujuan ketentuan hauling berhasil disimpan (NULL/FALSE -> TRUE)."
                    : "Data ketentuan hauling sudah TRUE sebelumnya.",
                certificateUrl
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ketentuan_hauling", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Kolom ketentuan_hauling belum tersedia pada tabel tb_simper."
            });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedColumn)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Kolom ketentuan_hauling belum tersedia pada tabel tb_simper."
            });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Tabel tb_simper belum tersedia pada database Bima."
            });
        }
        catch (NpgsqlException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "Koneksi database Bima sedang bermasalah."
            });
        }
    }

    [HttpGet("{reqId}/certificate-pdf")]
    public async Task<IActionResult> DownloadCertificatePdf(string reqId, CancellationToken cancellationToken)
    {
        var normalizedReqId = reqId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReqId))
        {
            return BadRequest("Req ID tidak valid.");
        }

        try
        {
            var lookup = await LoadPermitFromBimaAsync(normalizedReqId, cancellationToken);
            if (lookup is null || lookup.Permit is null)
            {
                return NotFound("Data permit tidak ditemukan.");
            }

            if (!lookup.IsHaulingCompleted)
            {
                return BadRequest("Sertifikat PDF hanya tersedia setelah ketentuan hauling selesai (TRUE).");
            }

            var vm = new PermitHaulingDetailViewModel
            {
                ReqId = normalizedReqId,
                Permit = lookup.Permit,
                IsHaulingCompleted = true,
                CertifiedAt = DateTimeOffset.UtcNow,
                CertificateId = BuildCertificateId(normalizedReqId),
                CertificateUrl = BuildHaulingCertificateUrl(normalizedReqId),
                CertificateQrDataUrl = BuildQrCodeDataUrl(BuildHaulingCertificateUrl(normalizedReqId))
            };

            var document = new HaulingCertificatePdfDocument(vm);
            var pdfBytes = document.GeneratePdf();
            var fileName = $"sertifikat-ketentuan-hauling-{SanitizeFileToken(normalizedReqId)}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (NpgsqlException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Koneksi database Bima sedang bermasalah.");
        }
    }

    private async Task<PermitLookupResult?> LoadPermitFromBimaAsync(string reqId, CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("BimaConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string BimaConnection/DefaultConnection belum dikonfigurasi.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var reqColumn = await ResolveSimperReqColumnAsync(connection, cancellationToken);
        var permitJoinColumn = await ResolveSimperPermitJoinColumnAsync(connection, cancellationToken);
        var hasKetentuanColumn = await HasColumnAsync(connection, "tb_simper", "ketentuan_hauling", cancellationToken);
        var reqExpression = reqColumn == "id"
            ? "s.id::text"
            : $"s.{reqColumn}::text";
        var ketentuanExpression = hasKetentuanColumn
            ? "COALESCE(s.ketentuan_hauling, FALSE)::text"
            : "'0'";
        var completionSortExpression = hasKetentuanColumn
            ? "CASE WHEN COALESCE(s.ketentuan_hauling, FALSE) THEN 1 ELSE 0 END"
            : "0";

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = $"""
            SELECT
                s.id AS simper_id,
                {reqExpression} AS req_id,
                p.id AS permit_id,
                p.tanggal,
                p.nomor,
                p.status,
                p.pengajuan,
                p.akses_lokasi,
                p.nik,
                p.nama,
                p.email,
                p.perusahaan,
                p.departemen,
                p.jabatan,
                p.posisi,
                {ketentuanExpression} AS ketentuan_hauling
            FROM public.tb_simper s
            LEFT JOIN public.tb_permit p ON p.id = s.{permitJoinColumn}
            WHERE {reqExpression} ILIKE @req_id
            ORDER BY {completionSortExpression} DESC, s.id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("req_id", reqId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var permit = new PermitApprovalView
        {
            Id = reader["permit_id"] is DBNull ? reader.GetInt64(reader.GetOrdinal("simper_id")) : reader.GetInt64(reader.GetOrdinal("permit_id")),
            ReqId = reader["req_id"] as string,
            Tanggal = reader["tanggal"] is DBNull ? null : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("tanggal")), DateTimeKind.Utc),
            Nomor = reader["nomor"] as string,
            Status = reader["status"] as string,
            Pengajuan = reader["pengajuan"] as string,
            AksesLokasi = reader["akses_lokasi"] as string,
            Nik = reader["nik"] as string,
            Nama = reader["nama"] as string,
            Email = reader["email"] as string,
            Perusahaan = reader["perusahaan"] as string,
            Departemen = reader["departemen"] as string,
            Jabatan = reader["jabatan"] as string,
            Posisi = reader["posisi"] as string
        };

        var isCompleted = IsCompletedValue(reader["ketentuan_hauling"]);
        return new PermitLookupResult(permit, isCompleted);
    }

    private async Task<HaulingUpdateResult> MarkKetentuanHaulingCompletedAsync(string reqId, CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("BimaConnection")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string BimaConnection/DefaultConnection belum dikonfigurasi.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var reqColumn = await ResolveSimperReqColumnAsync(connection, cancellationToken);
        var hasKetentuanColumn = await HasColumnAsync(connection, "tb_simper", "ketentuan_hauling", cancellationToken);
        if (!hasKetentuanColumn)
        {
            throw new InvalidOperationException("Kolom ketentuan_hauling tidak ditemukan.");
        }

        var reqExpression = reqColumn == "id"
            ? "s.id::text"
            : $"s.{reqColumn}::text";

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = $"""
            WITH target AS (
                SELECT s.id
                FROM public.tb_simper s
                WHERE {reqExpression} ILIKE @req_id
                ORDER BY s.id DESC
                LIMIT 1
            ),
            updated AS (
                UPDATE public.tb_simper s
                SET ketentuan_hauling = TRUE
                FROM target t
                WHERE s.id = t.id
                  AND s.ketentuan_hauling IS DISTINCT FROM TRUE
                RETURNING s.id
            )
            SELECT
                EXISTS (SELECT 1 FROM target) AS target_found,
                EXISTS (SELECT 1 FROM updated) AS updated;
            """;
        command.Parameters.AddWithValue("req_id", reqId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new HaulingUpdateResult(false, false);
        }

        var targetFound = reader.GetBoolean(reader.GetOrdinal("target_found"));
        var updated = reader.GetBoolean(reader.GetOrdinal("updated"));
        return new HaulingUpdateResult(targetFound, updated);
    }

    private static async Task<string> ResolveSimperReqColumnAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'tb_simper'
              AND column_name IN ('req_id', 'nomor', 'id')
            ORDER BY CASE column_name
                WHEN 'req_id' THEN 1
                WHEN 'nomor' THEN 2
                WHEN 'id' THEN 3
                ELSE 9
            END
            LIMIT 1;
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is string column && !string.IsNullOrWhiteSpace(column))
        {
            return column;
        }

        throw new InvalidOperationException("Kolom req id pada public.tb_simper tidak ditemukan (req_id/nomor/id).");
    }

    private static async Task<string> ResolveSimperPermitJoinColumnAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'tb_simper'
              AND column_name = 'permit_id'
            LIMIT 1;
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is string column && !string.IsNullOrWhiteSpace(column))
        {
            return column;
        }

        throw new InvalidOperationException("Kolom permit_id pada public.tb_simper tidak ditemukan.");
    }

    private static async Task<bool> HasColumnAsync(NpgsqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @table_name
              AND column_name = @column_name
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("table_name", tableName);
        command.Parameters.AddWithValue("column_name", columnName);

        var exists = await command.ExecuteScalarAsync(cancellationToken);
        return exists is not null;
    }

    private string BuildHaulingCertificateUrl(string reqId)
    {
        var routePath = $"/ketentuanjalanhauling/{Uri.EscapeDataString(reqId)}";
        var configuredBaseUrl = _configuration["App:PublicUrl"] ?? _configuration["App:Url"];
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var publicBaseUri)
            && !string.Equals(publicBaseUri.Host, "0.0.0.0", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(publicBaseUri, routePath).ToString();
        }

        return Url.Action(nameof(Detail), "Permit", new { reqId }, Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}{routePath}";
    }

    private static string BuildCertificateId(string reqId)
    {
        var raw = $"HAULING|{reqId.Trim().ToUpperInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var code = Convert.ToHexString(hash)[..10];
        return $"KH-{code}";
    }

    private static string BuildQrCodeDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var bytes = pngQr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private static bool IsCompletedValue(object? value)
    {
        if (value is null or DBNull)
        {
            return false;
        }

        var text = value.ToString()?.Trim().ToLowerInvariant();
        return text is "1" or "t" or "true" or "y" or "yes";
    }

    private static string SanitizeFileToken(string value)
    {
        var sanitized = new string(value
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');

        return string.IsNullOrWhiteSpace(sanitized) ? "req" : sanitized;
    }

    private sealed record PermitLookupResult(PermitApprovalView Permit, bool IsHaulingCompleted);
    private sealed record HaulingUpdateResult(bool TargetFound, bool Updated);
}
