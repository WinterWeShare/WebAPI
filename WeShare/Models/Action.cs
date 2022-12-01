﻿namespace WebAPI.Models;

public class Action
{
    public int Id { get; set; }

    public string ActionType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int AdminId { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}