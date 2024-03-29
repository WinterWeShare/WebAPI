﻿namespace WebAPI.Models.EntityFramework;

public class Invite
{
    public int Id { get; set; }

    public int ReceiverId { get; set; }

    public int SenderId { get; set; }

    public int GroupId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
