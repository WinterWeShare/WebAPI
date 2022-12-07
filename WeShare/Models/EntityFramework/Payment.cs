namespace WebAPI.Models.EntityFramework;

public partial class Payment
{
    public int Id { get; set; }

    public int UserToGroupId { get; set; }

    public string Title { get; set; } = null!;

    public double Amount { get; set; }

    public DateTime Date { get; set; }

    public virtual UserToGroup UserToGroup { get; set; } = null!;
}
