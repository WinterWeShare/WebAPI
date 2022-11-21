﻿using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class DeactivatedUser
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public bool ByAdmin { get; set; }

    public virtual User User { get; set; } = null!;
}
