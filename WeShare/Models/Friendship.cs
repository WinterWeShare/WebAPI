using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Friendship
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int FriendId { get; set; }

    public virtual User Friend { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
