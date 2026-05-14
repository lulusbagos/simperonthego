using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Keyless]
[Table("vw_permit_approval", Schema = "public")]
public class PermitApprovalView
{
    [Column("id")]
    public long Id { get; set; }

    [Column("tanggal")]
    public DateTime? Tanggal { get; set; }

    [Column("nomor")]
    public string? Nomor { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("pengajuan")]
    public string? Pengajuan { get; set; }

    [Column("akses_lokasi")]
    public string? AksesLokasi { get; set; }

    [Column("nik")]
    public string? Nik { get; set; }

    [Column("nama")]
    public string? Nama { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("perusahaan")]
    public string? Perusahaan { get; set; }

    [Column("departemen")]
    public string? Departemen { get; set; }

    [Column("jabatan")]
    public string? Jabatan { get; set; }

    [Column("posisi")]
    public string? Posisi { get; set; }

    [Column("req_id")]
    public string? ReqId { get; set; }
}
