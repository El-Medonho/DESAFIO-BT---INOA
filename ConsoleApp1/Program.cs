using System;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

public class Call {
    private static string token = "gVmotaaAMJLzifhZ1dozeM";
    private static string baseAddress = "https://brapi.dev/api/quote";
    private static HttpClient client = new(){};

    public static async Task<HttpResponseMessage?> GetAsync(string ticker) {
        var parameters = new Dictionary<string, string>
        {
            { "token", token }
        };

        var query = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
        string requestUrl = $"{baseAddress}/{ticker}?{query}";

        try {
            HttpResponseMessage response = await client.GetAsync(requestUrl);
            
            if (response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseData);
            }
            else
            {
                Console.WriteLine($"Request failed with status code {response.StatusCode}");
            }
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }
} 

public class APIResponse {
    public List<APIResults>? results { get; set; }
}

public class APIResults {
    public double regularMarketPrice { get; set; }
}
public class MainProgram {
    private static double lowerBound = 36.03, upperBound = 36.40;

    public static async Task Main() {

        while(true){
            HttpResponseMessage? response = await Call.GetAsync("PETR4");

            if(response is not null){
                string responseData = await response.Content.ReadAsStringAsync();

                double currentPrice = JsonSerializer.Deserialize<APIResponse>(responseData)!.results![0].regularMarketPrice;

                Console.WriteLine(currentPrice);

                if(currentPrice < lowerBound) {
                    Console.WriteLine("\nPreço de compra.\n");
                }
                else if(currentPrice > upperBound) {
                    Console.WriteLine("\nPreço de venda.\n");
                }
            }

            Console.WriteLine("Oi");

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

    }
}
