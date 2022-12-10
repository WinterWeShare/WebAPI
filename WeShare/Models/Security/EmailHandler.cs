using System.Net;
using System.Net.Mail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebAPI.Models.Security;

public class EmailHandler
{
    private readonly Account _account;

    public EmailHandler(string receiver)
    {
        ProjectPath = Directory.GetCurrentDirectory();
        // Get the email address and password from email.json at the root of the project
        var json = File.ReadAllText(Path.Combine(ProjectPath, "email.json"));
        _account = JsonConvert.DeserializeObject<Account>(json)!;
        Receiver = receiver;
        Code = new Random().Next(100000, 999999);
    }

    public int Code { get; }
    private string Receiver { get; }
    private string ProjectPath { get; }

    public void SendSessionKey()
    {
        var mail = new MailMessage();
        var smtpServer = new SmtpClient("smtp.gmail.com");
        mail.From = new MailAddress(_account.Email);
        mail.To.Add(Receiver);
        mail.Subject = "Two Factor Authentication";
        var body = @$"There was a new login attempt to your account at {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}.
If this wasn't you, change your password immediately.
Otherwise, enter the following code: {Code}";
        mail.Body = body;
        smtpServer.Port = 587;
        smtpServer.Credentials = new NetworkCredential($"{_account.Email}", $"{_account.Password}");
        smtpServer.EnableSsl = true;
        smtpServer.Send(mail);
    }

    public void SendRecoveryCodes(List<string> recoveryCodes)
    {
        var mail = new MailMessage();
        var smtpServer = new SmtpClient("smtp.gmail.com");
        mail.From = new MailAddress(_account.Email);
        mail.To.Add(Receiver);
        mail.Subject = "Two Factor Authentication Recovery Codes";
        var body = @$"Thank you for registering on our website.
Here are your recovery codes in case you forget your password, please keep them safe.
{string.Join("\n", recoveryCodes)}";
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
    
