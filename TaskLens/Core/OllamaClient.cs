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
                model = "gemma3:12b",
                prompt = $"프로세스 이름은 {processName}입니다. 어떤 프로세스 인가요? 짧게 알려주세요.",
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

        public static async Task PrimeModelStyleAsync ()
        {
            var request = new
            {
                model = "gemma3:latest",
                system = "너는 보안 전문가 AI야. 설명은 최대 1문장, 쉬운 말로." +
                " 당신이 알 수 없는 프로세스의 이름일 경우에는 [주의] 알 수 없는 프로세스. 라고 답해주세요.",
                prompt = "앞으로 입력될 모든 프로세스 이름에 대해 짧고 정확한 설명을 제공할 준비를 하세요.",
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:11434", content);
            response.EnsureSuccessStatusCode();
        }

    }
}
