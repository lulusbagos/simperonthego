using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Keyless]
[Table("vw_pengajuan_simper", Schema = "public")]
public class SimperApplicantView
{
    [Column("id")]
    public long Id { get; set; }

    [Column("tanggal")]
    public string? Tanggal { get; set; }

    [Column("nomor")]
    public string? Nomor { get; set; }

    [Column("ktp")]
    public string? Ktp { get; set; }

    [Column("pengajuan")]
    public string? Pengajuan { get; set; }

    [Column("nik")]
    public string? Nik { get; set; }

    [Column("nama")]
    public string? Nama { get; set; }

    [Column("perusahaan")]
    public string? Perusahaan { get; set; }

    [Column("departemen")]
    public string? Departemen { get; set; }

    [Column("berakhir_kerja")]
    public string? BerakhirKerja { get; set; }

    [Column("created_company_id")]
    public long? CreatedCompanyId { get; set; }

    [Column("created_department_id")]
    public long? CreatedDepartmentId { get; set; }
}
