using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CarInsuranceSalesBot.Services;

public class OpenAiService
{
    private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiService> _logger;
        private readonly IConfiguration _configuration;

        public OpenAiService(HttpClient httpClient, ILogger<OpenAiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                
                // Create OpenAI API request
                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = prompt
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/responses", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(jsonResponse);
                    return responseObject["choices"]?[0]?["message"]?["content"]?.ToString();
                }
                
                _logger.LogError("OpenAI API error: {ErrorMessage}", await response.Content.ReadAsStringAsync());
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return null;
            }
        }
}