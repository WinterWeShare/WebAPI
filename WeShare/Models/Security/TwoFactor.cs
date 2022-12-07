using System.Net;
using System.Net.Mail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebAPI.Models.Security;

public class TwoFactor
{
    private readonly Account _account;
    public int Code { get; }
    private string Receiver { get; }
    private string IpAddress { get; }
    private JObject Location { get; }
    private string ProjectPath { get; }

    public TwoFactor(string receiver)
    {
        // Get the project path, go up by three directories to get to the root of the project
        ProjectPath = Directory.GetCurrentDirectory();
        // Get the email address and password from email.json at the root of the project
        var json = File.ReadAllText(Path.Combine(ProjectPath, "email.json"));
        _account = JsonConvert.DeserializeObject<Account>(json)!;
        Receiver = receiver;
        Code = new Random().Next(100000, 999999);
        IpAddress = GetIp().Result;
        Location = GetLocation().Result;
    }

    private async Task<string> GetIp()
    {
        var externalIp = await new HttpClient().GetStringAsync("http://icanhazip.com");
        return externalIp;
    }

    private async Task<JObject> GetLocation()
    {
        var location = await new HttpClient().GetStringAsync("http://ip-api.com/json/" + IpAddress);
        return JObject.Parse(location);
    }

    public void SendCode()
    {
        MailMessage mail = new MailMessage();
        SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
        mail.From = new MailAddress(_account.Email);
        mail.To.Add(Receiver);
        mail.Subject = "Two Factor Authentication";
        var body = @$"There was a new login attempt to your account.
Ip: {IpAddress}Location: {Location["zip"]} {Location["city"]}, {Location["country"]}, {Location["regionName"]}
At: {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}.
If this was you, please enter the following code: {Code}";
        mail.Body = body;
        smtpServer.Port = 587;
        smtpServer.Credentials = new NetworkCredential($"{_account.Email}", $"{_account.Password}");
        smtpServer.EnableSsl = true;
        smtpServer.Send(mail);
    }

    private class Account
    {
        public Account(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; }
        public string Password { get; }
    }
}
    
