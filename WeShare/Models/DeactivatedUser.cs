namespace WebAPI.Models;

public class DeactivatedUser
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public bool ByAdmin { get; set; }

    public virtual User User { get; set; } = null!;
}