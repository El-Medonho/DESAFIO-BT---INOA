using System;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

public class Email {
    private static string token = "mlsn.895613bbcf08bba8d58cae0190ea4e8b9cdeea4310a4e9926bb0644ba103d9d7";

    private static string baseAddress = "https://api.mailersend.com/v1/email";

    public static async Task<bool> PostAsync(int type, string ticker, double price) {
        var parameters = new
        {
            from = new {
                email = "fred@trial-yxj6lj9x9z54do2r.mlsender.net"
            },

            to = new[] {
                new {
                    email = "bfredb.20221@poli.ufrj.br"
                }
            },
            
            subject = (type == -1 ? "Queda no preço do ativo " + ticker : "Aumento no preço do ativo " + ticker),
            
            text = (type == -1 ? 
            "O preço do ativo " + ticker + " está abaixo do preço de referência de compra, com preço atual de " + price + ".": 
            "O preço do ativo " + ticker + " está acima do preço de referência de venda, com preço atual de " + price + "."
            ),
        };

        string json = JsonSerializer.Serialize(parameters);

        using (HttpClient client = new HttpClient()) {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            
            var query = new StringContent(json, Encoding.UTF8, "application/json");

            try {
                HttpResponseMessage response = await client.PostAsync(baseAddress, query);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseData);
                    return true;
                }
                else
                {
                    Console.WriteLine($"Request failed with status code {response.StatusCode}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
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
    private static double lowerBound = 36.00, upperBound = 36.00;

    private static string ticker = "PETR4";

    public static async Task Main() {

        while(true){
            HttpResponseMessage? response = await Call.GetAsync("PETR4");

            if(response is not null){
                string responseData = await response.Content.ReadAsStringAsync();

                double currentPrice = JsonSerializer.Deserialize<APIResponse>(responseData)!.results![0].regularMarketPrice;

                Console.WriteLine(currentPrice);

                if(currentPrice < lowerBound) {
                    bool ok = await Email.PostAsync(-1, ticker, currentPrice);

                    if(!ok){
                        Console.WriteLine("Não foi enviada a notificação.");
                    }
                }
                else if(currentPrice > upperBound) {
                    bool ok = await Email.PostAsync(1, ticker, currentPrice);

                    if(!ok){
                        Console.WriteLine("Não foi enviada a notificação.");
                    }
                }
            }

            // Console.WriteLine("Oi");

            await Task.Delay(TimeSpan.FromSeconds(30));
        }

    }
}
