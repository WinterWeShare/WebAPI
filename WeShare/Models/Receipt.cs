﻿using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Receipt
{
    public int Id { get; set; }

    public int UserToGroupId { get; set; }

    public double Amount { get; set; }

    public bool Fulfilled { get; set; }

    public DateTime? Date { get; set; }

    public virtual UserToGroup UserToGroup { get; set; } = null!;
}
