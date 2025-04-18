using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskLens.Core
{
    public static class OllamaClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetProcessExplanationAsync(string processName)
        {
            var request = new
            {
                model = "llama3.2-vision:11b",
                prompt = $"프로세스 이름: {processName}\n이 프로세스가 어떤 용도인지 간단히 설명해줘. 짧게, 1문장.",
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(resultJson);
            var output = result.RootElement.GetProperty("response").GetString();

            return output?.Trim();
        }
    }
}
