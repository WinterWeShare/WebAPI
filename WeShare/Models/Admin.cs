﻿namespace WebAPI.Models;

public class Admin
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public virtual ICollection<Action> Actions { get; } = new List<Action>();
}