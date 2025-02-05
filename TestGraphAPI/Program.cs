// See https://aka.ms/new-console-template for more information

using EmailService;
GraphAPIEmail.SendEmail();

Console.WriteLine("Hello, World!");

public class GraphAPIEmail
{
    public static async void SendEmail()
    {
        var emailService = new GraphEmailService("clientId", "tenantId", "secretId");
        await emailService.SendEmailAsync(
            new[] { "recipient@example.com" },
            "Test Subject",
            "This is a test email.",
            bccEmails: new string[] { "BCC Emails" },
            ccEmails: new string[] { "CC Emails" },
            attachmentPaths: new List<string> { "test.pdf" }
        );
    }
}
