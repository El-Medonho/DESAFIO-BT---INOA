using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

public class Call {
    private static string token = "gVmotaaAMJLzifhZ1dozeM";
    private static string baseAddress = "https://brapi.dev/api/quote/PETR4";
    private static HttpClient client = new(){};

    public static async Task GetAsync() {
        var parameters = new Dictionary<string, string>
        {
            { "token", token }
        };

        var query = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
        string requestUrl = $"{baseAddress}?{query}";

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
} 

public class MainProgram {
    public static async Task Main() {
        await Call.GetAsync();
    }
}