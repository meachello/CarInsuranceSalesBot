using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.Logging;     
using System.Net.Http.Headers;          

namespace CarInsuranceSalesBot.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly IConfiguration _configuration;

        public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("API key is missing (Gemini:ApiKey).");
                    return null;
                }

                var endpoint =
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
                
                // Create OpenAI API request
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };
                
            var jsonPayload = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = httpContent
            };

            _logger.LogInformation("Sending request to Gemini API: {Endpoint}", endpoint);
            var response = await _httpClient.SendAsync(requestMessage);
            
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Request successful. Status Code: {StatusCode}", response.StatusCode);
                try
                {
                    var responseObject = JObject.Parse(responseBody);
                    
                    var candidate = responseObject?["candidates"]?.FirstOrDefault();
                    var contentPart = candidate?["content"]?["parts"]?.FirstOrDefault();
                    var generatedText = contentPart?["text"]?.ToString();
                    
                    if (string.IsNullOrEmpty(generatedText))
                    {
                        var promptFeedback = responseObject?["promptFeedback"];
                        var finishReason = candidate?["finishReason"]?.ToString();
                        _logger.LogWarning("Ответ Gemini успешен, но не содержит текстового контента или он пуст. Finish Reason: {FinishReason}. Prompt Feedback: {PromptFeedback}. Полный ответ: {ResponseBody}", finishReason, promptFeedback?.ToString(Formatting.None), responseBody);
                        
                        if (finishReason == "SAFETY")
                        {
                            return "Ответ заблокирован из-за настроек безопасности.";
                        }
                    }

                    return generatedText;
                }
                catch (JsonReaderException jsonEx)
                {
                    _logger.LogError(jsonEx, "Не удалось разобрать JSON успешного ответа от Gemini. Тело ответа: {ResponseBody}", responseBody);
                    return null;
                }
                catch (Exception ex) // Ловим другие возможные ошибки при обработке ответа
                {
                     _logger.LogError(ex, "Ошибка при обработке успешного ответа от Gemini. Тело ответа: {ResponseBody}", responseBody);
                     return null;
                }
            }
            else
            {
                // Логируем детальную информацию об ошибке
                _logger.LogError("Запрос к Gemini API завершился ошибкой. Status Code: {StatusCode}. Тело ответа: {ResponseBody}", response.StatusCode, responseBody);
                // При необходимости можно попытаться разобрать тело ошибки для получения деталей
                // try {
                //    var errorObject = JObject.Parse(responseBody);
                //    var errorMessage = errorObject?["error"]?["message"]?.ToString();
                //    _logger.LogError("Сообщение об ошибке от Gemini API: {ErrorMessage}", errorMessage);
                // } catch {} // Игнорировать ошибки парсинга тела ошибки
                return null;
            }
        }
        catch (HttpRequestException httpEx) // Ошибки сети или DNS
        {
             _logger.LogError(httpEx, "Ошибка HTTP запроса при вызове Gemini API.");
             return null;
        }
        catch (Exception ex) // Другие непредвиденные исключения
        {
            _logger.LogError(ex, "Произошла непредвиденная ошибка в GenerateResponseAsync");
            return null;
        }
        }
}