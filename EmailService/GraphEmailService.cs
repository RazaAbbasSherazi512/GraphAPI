using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.IO;
using EmailService.Models;
using System.Linq;
using System.Collections.Generic;
using EmailService.Utilities;

namespace EmailService
{
    public sealed class GraphEmailService
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _secretId;
        private readonly string _redirectUri;
        private readonly string[] _scopes;
        private readonly string _token;
        private readonly TokenService _tokenService;
        public GraphEmailService(string clientId, string tenantId, string secretId = null, string redirectUri = "http://localhost", string[] scopes = null, string token = null)
        {
            _token = token;
            _tokenService = new TokenService(clientId, tenantId, secretId, redirectUri, scopes, token);
            _clientId = clientId;
            _tenantId = tenantId;
            _redirectUri = redirectUri;
            _secretId = secretId;

            // Initialize the default scopes
            string[] defaultScopes = new[] { "Mail.Send" };

            _scopes = scopes != null && scopes.Any()
                ? defaultScopes.Concat(scopes).ToArray()
                : defaultScopes;
        }

        public async Task<EmailResultResponseDTO> SendEmailAsync(string[] recipientEmails, string subject, string body, string[] ccEmails = null, string[] bccEmails = null, List<string> attachmentPaths = null)
        {
            try
            {
                string accessToken = await _tokenService.GetAccessTokenAsync();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // To Recipients
                    var toRecipients = recipientEmails.Select(email => new
                    {
                        emailAddress = new { address = email }
                    }).ToArray();

                    // CC Recipients
                    var ccRecipients = ccEmails?.Select(email => new
                    {
                        emailAddress = new { address = email }
                    }).ToArray() ?? new object[0];

                    // BCC Recipients 
                    var bccRecipients = bccEmails?.Select(email => new
                    {
                        emailAddress = new { address = email }
                    }).ToArray() ?? new object[0];

                    // Attachments 
                    var attachments = attachmentPaths?.Select(path => new Dictionary<string, object>
                                    {
                                        { "@odata.type", "#microsoft.graph.fileAttachment" },
                                        { "name", Path.GetFileName(path) },
                                        { "contentBytes", Convert.ToBase64String(File.ReadAllBytes(path)) },
                                        { "contentType", FileHelper.GetMimeType(path) }
                                    }).ToArray() ?? new object[0];

                    var requestBody = new
                    {
                        saveToSentItems = true,
                        message = new
                        {
                            subject = subject,
                            body = new
                            {
                                contentType = "Text",
                                content = body,
                            },
                            toRecipients = toRecipients,
                            ccRecipients = ccRecipients,
                            bccRecipients = bccRecipients,
                            attachments = attachments,
                            hasAttachments = attachments.Any()
                        }
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://graph.microsoft.com/v1.0/me/sendMail", content);

                    if (response.IsSuccessStatusCode)
                    {
                        return new EmailResultResponseDTO { IsSuccess = true, ErrorMessage = null, StatusCode = StatusCode.Succeeded, RefreshToken = _token != accessToken, Token = accessToken };
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var errorMessage = $"Error sending email: {response.StatusCode} - {response.ReasonPhrase}. Details: {responseBody}";
                        return new EmailResultResponseDTO { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = StatusCode.Failed };
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}

