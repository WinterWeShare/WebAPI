namespace WebAPI.Models.EntityFramework;

public class UserPassword
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Password { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
