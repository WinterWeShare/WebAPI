using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class ToBePaid
{
    public int Id { get; set; }

    public int UserToGroupId { get; set; }

    public bool Approved { get; set; }

    public DateTime? Date { get; set; }

    public virtual UserToGroup UserToGroup { get; set; } = null!;
}
