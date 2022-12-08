namespace WebAPI.Models.EntityFramework;

public class AdminSession
{
    public int Id { get; set; }

    public string SessionKey { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public int AdminId { get; set; }

    public DateTime Date { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
