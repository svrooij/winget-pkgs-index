
using System.ComponentModel.DataAnnotations.Schema;

namespace WingetIndexGenerator.Models;
public partial class TagsMap
{
    [Column("package")]
    public long PackageId { get; set; }
    [Column("tag")]
    public long TagId { get; set; }
}