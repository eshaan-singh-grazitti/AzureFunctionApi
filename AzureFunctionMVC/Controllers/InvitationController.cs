using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using AzureFunctionMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AzureFunctionMVC.Controllers
{
    public class InvitationController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;
        private readonly string blobConnectionString = "UseDevelopmentStorage=true"; // Emulator connection
        private readonly string containerName = "invitations"; // Your container name
        private readonly string templateContainerName = "templates";

        public InvitationController(HttpClient httpClient, IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GenerateInvitation([FromBody] GuestModel model)
        {
            try
            {
                // Fetch HTML template from Blob Storage
                string blobName = "InvitationTemplate.html"; // Ensure this matches the blob filename
                BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(templateContainerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    return BadRequest("Template file not found in Blob Storage.");
                }

                BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
                string htmlTemplate = downloadResult.Content.ToString();

                // Prepare request payload
                var payload = new
                {
                    GuestName = model.GuestName,
                    Designation = model.Designation,
                    HtmlTemplate = htmlTemplate
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8, "application/json");

                // Call Azure Function
                var response = await _httpClient.PostAsync("http://localhost:7150/api/GenerateGuestPDF", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Invitation generated!");
                }
                else
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return BadRequest($"Azure Function Error: {errorDetails}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode(500, $"HTTP Error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
        public async Task<IActionResult> ShowInvitations()
        {
            List<string> fileUrls = new List<string>();
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                // Generate URL for the file (emulator runs on http://127.0.0.1:10000/devstoreaccount1)
                string fileUrl = $"http://127.0.0.1:10000/devstoreaccount1/{containerName}/{blobItem.Name}";
                fileUrls.Add(fileUrl);
            }

            return View(fileUrls);
        }
    }
}
