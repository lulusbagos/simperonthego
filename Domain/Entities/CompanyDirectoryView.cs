using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SimperSecureOnlineTestSystem.Domain.Entities;

[Keyless]
[Table("vw_company", Schema = "public")]
public class CompanyDirectoryView
{
    [Column("id")]
    public long Id { get; set; }

    [Column("code")]
    public string? Code { get; set; }

    [Column("company_name")]
    public string? CompanyName { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("deleted_by")]
    public long? DeletedBy { get; set; }

    [Column("created_at")]
    public string? CreatedAt { get; set; }

    [Column("updated_at")]
    public string? UpdatedAt { get; set; }

    [Column("deleted_at")]
    public string? DeletedAt { get; set; }

    [Column("company_email")]
    public string? CompanyEmail { get; set; }
}
