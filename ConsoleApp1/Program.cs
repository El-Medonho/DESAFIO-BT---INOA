using System;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public static class Call {
    private static string token = Program.secret["ApiSettings:TokenP"]!;
    private static string baseAddress = "https://brapi.dev/api/quote";
    private static HttpClient client = new(){};

    //função para chamar os dados do ativo requisitado
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
                if(response.StatusCode.ToString() == "404") Console.WriteLine("Verifique se o ativo foi passado corretamente.");
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
    private static string token = Program.secret["ApiSettings:TokenE"]!;

    private static string baseAddress = "https://api.mailersend.com/v1/email";

    //função para enviar o email
    public static async Task<bool> PostAsync(int type, string ticker, double price) {
        var parameters = new
        {
            from = new {
                email = Program.config["EmailSettings:From"]
            },

            to = new[] {
                new {
                    email = Program.config["EmailSettings:To"]
                }
            },
            
            subject = (type == -1 ? "Queda no preço do ativo " + ticker : type == 1 ? "Aumento no preço do ativo " + ticker : "Atualização do preço do ativo " + ticker),
            
            text = (type == -1 ? 
            "O preço do ativo " + ticker + " está abaixo do preço de referência de compra, com preço atual de " + price + ".": type == 1 ? 
            "O preço do ativo " + ticker + " está acima do preço de referência de venda, com preço atual de " + price + "." : 
            "O preço do ativo " + ticker + " retornou a estar entre os preços de referência de compra e venda, com preço atual de " + price + "."
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

//classes feitas para pegar o regularMarketPrice do ativo da resposta da API
public class APIResponse {
    public List<APIResults>? results { get; set; }
}

public class APIResults {
    public double regularMarketPrice { get; set; }
}

//classe principal do programa
public class Program {
    public static IConfiguration secret = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

    public static IConfiguration config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json").Build();


    private static double INFlowerBound = 1e15, INFupperBound = 1e16; 
    private static double returnDelta = 0.01, priceDelta = 0.05; 


    public static async Task Main(string[] args) {
        string ticker = "PETR4";
        
        int state = 0;

        foreach(string s in args){
            Console.WriteLine(s);
        }
        
        double lowerBound = INFlowerBound, upperBound = INFupperBound;

        if(args.Length == 0){
            Console.WriteLine("Insira o ativo a ser monitorado como argumento.");
            return;
        }

        ticker = args[0];

        if(args.Length == 2){
            Console.WriteLine("É necessário informar o preço de referência de compra e venda como argumento.");
            return;
        }

        if(args.Length == 3){
            try{
                lowerBound = double.Parse(args[1]);
                upperBound = double.Parse(args[2]);
            }
            catch (Exception ex){
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Erro ao ler os preços de referência de compra e venda.");
                return;
            }
        }

        if(upperBound < lowerBound && Math.Abs(upperBound - lowerBound) > 1e-4){
            Console.WriteLine("Preço de referência de venda menor do que preço de referência de compra.");
            return;
        }

        if(lowerBound < 0) {
            Console.WriteLine("Preço de referência de compra negativo.");
            return;
        }

        while(true){
            HttpResponseMessage? response = await Call.GetAsync(ticker);

            if(response == null || !response.IsSuccessStatusCode) return;

            else if(response is not null){
                string responseData = await response.Content.ReadAsStringAsync();

                double currentPrice = JsonSerializer.Deserialize<APIResponse>(responseData)!.results![0].regularMarketPrice;

                // Caso não seja informado os valores de referência, inicializar com valores baseados no preço atual com um delta

                if(Math.Abs(INFlowerBound - lowerBound) < 1e-4) lowerBound = currentPrice - currentPrice*priceDelta;
                if(Math.Abs(INFupperBound - upperBound) < 1e-4) upperBound = currentPrice + currentPrice*priceDelta;

                Console.WriteLine(currentPrice);

                if(currentPrice < lowerBound && state != -1) {
                    bool ok = await Email.PostAsync(-1, ticker, currentPrice);

                    state = -1;

                    if(!ok){
                        Console.WriteLine("Não foi enviada a notificação.");
                    }
                    else Console.WriteLine("Foi enviada a notificação.");
                }
                else if(currentPrice > upperBound && state != 1) {
                    bool ok = await Email.PostAsync(1, ticker, currentPrice);
                    state = 1;

                    if(!ok){
                        Console.WriteLine("Não foi enviada a notificação.");
                    }
                    else Console.WriteLine("Foi enviada a notificação.");
                }

                //check para evitar enviar muitos emails caso o valor fique flutuando ao redor do delta
                else if(state != 0 && ((state == 1 && upperBound - currentPrice > returnDelta * upperBound) || (state == -1 && currentPrice - lowerBound > returnDelta * lowerBound) )) {
                    state = 0;

                    bool ok = await Email.PostAsync(0, ticker, currentPrice);

                    if(!ok){
                        Console.WriteLine("Não foi enviada a notificação.");
                    }
                    else Console.WriteLine("Foi enviada a notificação.");
                }
            }

            //isso faz com que ele faça as chamadas da API a cada 30 segundos
            await Task.Delay(TimeSpan.FromSeconds(30));

            //isso faz com que ele aguarde caso o mercado esteja fechado
            while(DateTime.Now.TimeOfDay.Hours <= 7 || DateTime.Now.TimeOfDay.Hours >= 18){
                Console.WriteLine($"Mercado brasileiro fechado. Esperando abrir para continuar a monitorar o ativo. Horário atual {DateTime.Now.TimeOfDay.Hours}:{DateTime.Now.TimeOfDay.Minutes}:{DateTime.Now.TimeOfDay.Seconds}");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

    }
}
