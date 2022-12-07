namespace WebAPI.Models.EntityFramework;

public partial class Wallet
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public double Balance { get; set; }

    public virtual User User { get; set; } = null!;
}
