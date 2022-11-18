namespace WebAPI.Models;

public class Payment
{
    public int Id { get; set; }

    public int UserToGroupId { get; set; }

    public double Amount { get; set; }

    public DateTime Date { get; set; }

    public virtual UserToGroup UserToGroup { get; set; } = null!;
}