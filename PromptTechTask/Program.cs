using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PromptTechTask
{

    class Program
    {
        static string Provider = "huggingface";
        static string Model = "openai/gpt-oss-20b"; // غيّره لو عايز موديل تاني
        static string OutDir = "week1_runs";
        static string ApiKey = "my key";
        static async Task Main(string[] args)
        {
            Directory.CreateDirectory(OutDir);


            string prompt = "Find the latest EV market trends. First list keywords, then call a search API, observe results, refine, and repeat until most relevant, recent data is found.";
                string output = await RunAsync(prompt);

            LogResult("react", "example", prompt, output);
            Console.WriteLine("\n Output:\n" + output);
        }

        static async Task<string> RunAsync(string prompt)
        {
            if (Provider == "huggingface")
                return await LlmHuggingFace(prompt);
            else
                throw new NotImplementedException("Provider not supported.");
        }

        // --- التعامل مع Hugging Face API ---
        static async Task<string> LlmHuggingFace(string prompt)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", ApiKey);

            var messages = new[] { new { role = "user", content = prompt } 
            ,new {role= "system", content = "You are a helpful assistant that provides concise and clear explanations." }
            };

            var payload = new
            {
                messages,
                model = Model,
                stream=false
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"https://router.huggingface.co/v1/chat/completions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode}\n{err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

            

                if (root.ValueKind == JsonValueKind.Object)
                {
                   
                    if (root.TryGetProperty("choices", out var txt))
                    {
                       
                      if(txt[0].TryGetProperty("message",out var r)){
                            var msg = r.GetProperty("content").GetString();
                            return msg;
                        }
                    }
                    if (root.TryGetProperty("error", out var err))
                        return "API Error: " + err.GetString();
                }

                return responseJson;
            }
            catch
            {
                return responseJson; 
            }
        }

        static void LogResult(string kind, string name, string prompt, string output)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string basePath = Path.Combine(OutDir, $"{kind}_{name}_{timestamp}");

            File.WriteAllText($"{basePath}.prompt.txt", prompt);
            File.WriteAllText($"{basePath}.output.txt", output);

            Console.WriteLine($" Logged → {OutDir}");
        }
    }


}
