using IronPdf;

namespace WebAPI.Models.Invoice;

public class PdfManager
{
    public static byte[] FormatInvoice(Invoice invoice)
    {
        var renderer = new HtmlToPdf();
        var html = $@"<body>
                <h1>Invoice for receipt #{invoice.Receipt.Id}</h1>
                <h2>Customer details</h2>
                <h3>Name: {invoice.User.FirstName} {invoice.User.LastName}</h3>
                <h3>Email: {invoice.User.Email}</h3>
                <h3>Phone: {invoice.User.PhoneNumber}</h3>
                <table>
                <h2>Payments</h2>
                    <tr>
                        <th>Title</th>
                        <th>Date</th>
                        <th>Amount</th>
                    </tr>";

        invoice.Payments.ForEach(payment =>
        {
            html += $"<tr><td>{payment.Title}</td><td>{payment.Date}</td><td>{payment.Amount}</td></tr>";
        });
        html += $@"</table>
                <h4>Receipt has been fulfilled at {invoice.Receipt.Date} for the total amount of {invoice.Receipt.Amount}</h4>
                </body>";
        return renderer.RenderHtmlAsPdf(html).BinaryData;
    }
}