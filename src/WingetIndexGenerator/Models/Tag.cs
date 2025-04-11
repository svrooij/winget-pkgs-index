
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WingetIndexGenerator.Models;
public partial class Tag
{
    public long Rowid { get; set; }
    public string? TagValue { get; set; }

    public override string ToString()
    {
        return TagValue ?? "";
    }
}

public partial class Tag
{
    public virtual ICollection<Package>? Packages { get; set; }
}

