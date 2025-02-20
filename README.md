Microsoft Graph API provides a powerful way to send emails programmatically using Office 365 and Azure. In this article, we will walk through the process of configuring OAuth authentication in Azure, obtaining the necessary credentials, and implementing a C# solution to send emails using Graph API.

If you’re not a Medium member, you can read the full article here:
Read on Medium

🔹 Step 1: Register an App in Azure AD
Before we can send emails using Microsoft Graph API, we need to register an application in Azure Active Directory (Azure AD) to obtain authentication credentials.

1️⃣ Navigate to Azure Portal
Open Azure Portal and log in.
Go to Azure Active Directory > App registrations.
Click New registration.
2️⃣ Register Your App
Enter a name for your app (e.g., Graph Email Sender).
Under Supported account types, select:
Single tenant (for internal use within one organization).
Multi-tenant (for multiple organizations).
Accounts in any directory and personal accounts (if needed).
Click Register.
3️⃣ Copy Client ID and Tenant ID
After registration, go to Overview and copy the following:

Application (client) ID → This is your Client ID.
Directory (tenant) ID → This is your Tenant ID.
🔹 Step 2: Configure API Permissions
To send emails, our application must have the correct API permissions.

1️⃣ Add Microsoft Graph Permissions
In Azure AD > App registrations, select your app.
Click API permissions > Add a permission.
Select Microsoft Graph.
Click Application permissions > Search for Mail.Send.
Select Mail.Send and click Add permissions.
2️⃣ Grant Admin Consent
After adding the permission, click Grant admin consent.
This will allow your app to send emails on behalf of users.
🔹 Step 3: Generate a Client Secret
Our application needs a Client Secret to authenticate.

Go to Certificates & secrets.
Click New client secret.
Provide a description and expiration period.
Click Add and copy the value (this is your Client Secret).
⚠️ Important: Store the client secret securely, as it cannot be retrieved later.

🔹 Step 4: Implement Email Sending in C#
Now that we have our Client ID, Tenant ID, and Client Secret, let’s write C# code to send emails using Microsoft Graph API.

📌 1. Create a DTO for Email Response
public class EmailResultResponseDTO
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public string Token { get; set; }
    public bool RefreshToken { get; set; }
    public StatusCode StatusCode { get; set; }
}
public enum StatusCode
{
    Succeeded,
    Failed,
    InvalidClientOrTenantId,
    DataNotFound
}
📌 2. Create a Helper Class for File Attachments
internal sealed class FileHelper
{
    internal static string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream",
        };
    }
}
📌 3. Implement the Token Service to Authenticate with Azure AD
using Microsoft.Identity.Client;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace EmailService
{
    public class TokenService
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _secretId;
        private readonly string[] _scopes;
        private IPublicClientApplication _app;
        public TokenService(string clientId, string tenantId, string secretId, string[] scopes)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _secretId = secretId;
            _scopes = scopes;
            _app = PublicClientApplicationBuilder.Create(_clientId)
                .WithTenantId(_tenantId)
                .WithClientSecret(_secretId)
                .Build();
        }
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var result = await _app.AcquireTokenForClient(_scopes).ExecuteAsync();
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting access token: " + ex.Message);
            }
        }
    }
}
📌 4. Implement Email Sending via Microsoft Graph API
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EmailService
{
    public sealed class GraphEmailService
    {
        private readonly TokenService _tokenService;
        public GraphEmailService(string clientId, string tenantId, string secretId)
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            _tokenService = new TokenService(clientId, tenantId, secretId, scopes);
        }
        public async Task<EmailResultResponseDTO> SendEmailAsync(string[] recipients, string subject, string body, List<string> attachments = null)
        {
            try
            {
                string accessToken = await _tokenService.GetAccessTokenAsync();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var toRecipients = recipients.Select(email => new { emailAddress = new { address = email } }).ToArray();
                    var emailAttachments = attachments?.Select(path => new
                    {
                        "@odata.type" = "#microsoft.graph.fileAttachment",
                        "name" = Path.GetFileName(path),
                        "contentBytes" = Convert.ToBase64String(File.ReadAllBytes(path)),
                        "contentType" = FileHelper.GetMimeType(path)
                    }).ToArray();
                    var requestBody = new
                    {
                        message = new
                        {
                            subject,
                            body = new { contentType = "Text", content = body },
                            toRecipients,
                            attachments = emailAttachments
                        }
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("https://graph.microsoft.com/v1.0/me/sendMail", content);
                    return response.IsSuccessStatusCode 
                        ? new EmailResultResponseDTO { IsSuccess = true, StatusCode = StatusCode.Succeeded }
                        : new EmailResultResponseDTO { IsSuccess = false, StatusCode = StatusCode.Failed, ErrorMessage = await response.Content.ReadAsStringAsync() };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending email: " + ex.Message);
            }
        }
    }
}

Executing the Email Service
We call the service from our main program:
public class GraphAPIEmail
{
    public static async void SendEmail()
    {
        var emailService = new GraphEmailService("clientId", "tenantId", "secretId");
        await emailService.SendEmailAsync(
            new[] { "recipient@example.com" },
            "Test Subject",
            "This is a test email.",
            new List<string> { "test.pdf" }
        );
    }
}
🚀 Conclusion
We successfully integrated Microsoft Graph API in C# to send emails. By following the steps to configure Azure AD, obtaining authentication credentials, and using Graph API, you can automate email sending securely.
