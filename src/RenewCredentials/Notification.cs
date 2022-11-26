using System;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class Notification
{
    [FunctionName("Notification")]
    public void Run([QueueTrigger("notifications", Connection = "AzureWebJobsStorage")] string notification,
        ILogger log)
    {
        var message = $"Notification: {notification}";
        
        var endpoint = Environment.GetEnvironmentVariable("SlackEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            // No endpoint configured, so just log the message
            log.LogWarning("SlackEndpoint is not set");
            return;
        }
        
        // send a slack message  
        using var client = HttpClientFactory.Create();
        using var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri(endpoint);
        request.Content = new StringContent(notification, Encoding.UTF8, "application/json");
        using var response = client.SendAsync(request).Result;
        response.EnsureSuccessStatusCode();
    }
}