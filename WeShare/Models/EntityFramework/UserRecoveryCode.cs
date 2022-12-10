namespace WebAPI.Models.EntityFramework;

public class UserRecoveryCode
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public bool Used { get; set; }

    public DateTime? Date { get; set; }

    public virtual User User { get; set; } = null!;
}
