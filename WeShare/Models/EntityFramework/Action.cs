namespace WebAPI.Models.EntityFramework;

public partial class Action
{
    public int Id { get; set; }

    public string ActionType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int AdminId { get; set; }

    public DateTime Date { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
