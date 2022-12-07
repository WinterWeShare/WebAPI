namespace WebAPI.Models.EntityFramework;

public partial class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime Date { get; set; }

    public bool Closed { get; set; }

    public virtual ICollection<Invite> Invites { get; } = new List<Invite>();

    public virtual ICollection<UserToGroup> UserToGroups { get; } = new List<UserToGroup>();
}
