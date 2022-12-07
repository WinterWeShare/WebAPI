namespace WebAPI.Models.EntityFramework;

public partial class UserSession
{
    public int Id { get; set; }

    public string SessionKey { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public int UserId { get; set; }

    public DateTime Date { get; set; }

    public virtual User User { get; set; } = null!;
}
