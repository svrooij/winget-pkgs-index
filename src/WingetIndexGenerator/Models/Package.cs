using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WingetIndexGenerator.Models;

public partial class Package
{
    public long Rowid { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Moniker { get; set; }

    [Column("latest_version")]
    public string? LatestVersion { get; set; }

    [Column("arp_min_version")]    
    public string? ArpMinVersion { get; set; }

    [Column("arp_max_version")]    
    public string? ArpMaxVersion { get; set; }

    public Byte[]? Hash { get; set; }

    public override string ToString()
    {
        return $"{Id} [{LatestVersion}]";
    }
}

public partial class Package
{
    public virtual ICollection<Tag>? Tags { get; set; }
}

