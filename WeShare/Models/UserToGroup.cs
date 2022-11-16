using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class UserToGroup
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GroupId { get; set; }

    public bool IsOwner { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; } = new List<Payment>();

    public virtual ICollection<Receipt> Receipts { get; } = new List<Receipt>();

    public virtual ICollection<ToBePaid> ToBePaids { get; } = new List<ToBePaid>();

    public virtual User User { get; set; } = null!;
}
