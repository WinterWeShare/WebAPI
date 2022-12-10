namespace WebAPI.Models.EntityFramework;

public class AdminPassword
{
    public int Id { get; set; }

    public int AdminId { get; set; }

    public string Password { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public virtual Admin Admin { get; set; } = null!;
}
