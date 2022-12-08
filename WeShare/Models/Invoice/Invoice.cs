using WebAPI.Models.EntityFramework;

namespace WebAPI.Models.Invoice;

public class Invoice
{
    public User User { get; set; }
    public Group Group { get; set; }
    public Wallet Wallet { get; set; }
    public Receipt Receipt { get; set; }
    public List<Payment> Payments { get; set; }
}