using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PdfTextExtractor
{
    // Request payload classes
    public class RequestPayload
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; } = Array.Empty<Message>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public ContentItem[] Content { get; set; } = Array.Empty<ContentItem>();
    }

    public class ContentItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("image_url")]
        public ImageUrl? ImageUrl { get; set; }
    }

    public class ImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    // Response payload classes
    public class ApiResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public ResponseMessage Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class ResponseMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string LM_STUDIO_URL = "http://localhost:1234/v1/chat/completions";

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SamplePDFScanner <path-to-pdf-file>");
                return;
            }

            string pdfPath = args[0];

            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Error: File '{pdfPath}' not found.");
                return;
            }

            if (!Path.GetExtension(pdfPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: File must be a PDF.");
                return;
            }

            try
            {
                Console.WriteLine($"Processing PDF: {pdfPath}");
                string extractedText = await ExtractTextFromPdf(pdfPath);
                
                Console.WriteLine("\n--- Extracted Text ---");
                Console.WriteLine(extractedText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task<string> ExtractTextFromPdf(string pdfPath)
        {
            // Convert PDF to base64
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);
            string base64Pdf = Convert.ToBase64String(pdfBytes);

            // Create the request payload using strongly-typed classes
            var requestPayload = new RequestPayload
            {
                Model = "qwen3-32b", // Change this to your model name
                Messages = new[]
                {
                    new Message
                    {
                        Role = "user",
                        Content = new[]
                        {
                            new ContentItem
                            {
                                Type = "text",
                                Text = "Please extract all the text elements from this PDF file. Return only the extracted text content without any additional formatting or commentary."
                            },
                            new ContentItem
                            {
                                Type = "image_url",
                                ImageUrl = new ImageUrl
                                {
                                    Url = $"data:application/pdf;base64,{base64Pdf}"
                                }
                            }
                        }
                    }
                },
                MaxTokens = 4000,
                Temperature = 0.1
            };

            // Serialize with proper options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(LM_STUDIO_URL, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"LMStudio API error: {response.StatusCode} - {errorContent}");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                
                // Deserialize using strongly-typed classes
                ApiResponse? apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, jsonOptions);
                
                if (apiResponse?.Choices?.Length > 0)
                {
                    string extractedText = apiResponse.Choices[0].Message.Content;
                    
                    // Optional: Display token usage info
                    if (apiResponse.Usage != null)
                    {
                        Console.WriteLine($"Tokens used: {apiResponse.Usage.TotalTokens} (prompt: {apiResponse.Usage.PromptTokens}, completion: {apiResponse.Usage.CompletionTokens})");
                    }
                    
                    return extractedText ?? "No text extracted.";
                }
                
                return "No response received from the model.";
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Failed to connect to LMStudio. Make sure it's running on localhost:1234. Details: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse API response: {ex.Message}");
            }
        }
    }
}