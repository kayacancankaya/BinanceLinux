using Binance;
using System.Collections.ObjectModel;
using Binance.Common;
using Binance.Spot;
using Binance.Spot.Models;
using Newtonsoft.Json;
using Convert = System.Convert;
using System.Drawing;
using BinanceLinux;

class Program
{
    static async Task Main(string[] args)
    {
        //GetTradeInfo("STGUSDT");
        //GetMinNotional("STGUSDT");
        //Define a timer with a 30 - seconds interval
        System.Timers.Timer timer = new System.Timers.Timer(10000*100);
        timer.Elapsed += async (sender, e) => await FetchBTCPrice();
        timer.AutoReset = true;
        timer.Start();
        await FetchBTCPrice();

        // Keep the application running
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        timer.Dispose();

    }
    static bool isRunning = false;
    static async Task FetchBTCPrice()
    {
        if (isRunning) return;
        try
        {
            isRunning = true;
            string baseUrl = "https://api.binance.com";
            string symbol = "BTCUSDT";//primaryCoin + secondaryCoin; 
            string endpoint = $"/api/v3/ticker/price?symbol={symbol}";

            dynamic json = null;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUrl);

                var response = await httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                json = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
            }
            if (json == null) { Console.WriteLine("Can not reach price api"); return; }

            ObservableCollection<Coin> lastProcess = Coin.GetCurrentPrice("BTC", "USDT");
            if (lastProcess.Count == 0) { Console.WriteLine("Can not get stored data"); return; }

            Coin coin = lastProcess.FirstOrDefault();

            coin.CurrentPrice = System.Convert.ToSingle(json.price);

            //birinci koşul son satılan fiyatın x kadar üstüne çıktı sat ath
            if (coin.LastSoldPrice <= coin.CurrentPrice - (coin.CurrentPrice * 0.0045))
                await PlaceOrder("BTC", "USDT", coin.CurrentPrice, true, "MARKET", 0.0026m, coin);

            //son satılanın x kadar altında son işlem satış
            else if (coin.CurrentPrice + (coin.CurrentPrice * 0.0045) <= coin.LastSoldPrice && coin.LastProcessIsSell)
                await PlaceOrder("BTC", "USDT", coin.CurrentPrice, false, "MARKET", 0.0026m, coin);
            //son satın alınanın x kadar üstünde son işlem alış
            else if (coin.CurrentPrice - (coin.CurrentPrice * 0.0045) >= coin.LastBoughtPrice && !coin.LastProcessIsSell)
                await PlaceOrder("BTC", "USDT", coin.CurrentPrice, true, "MARKET", 0.0026m, coin);

            //son alınan fiyatın x kadar altında atl
            else if (coin.LastBoughtPrice >= coin.CurrentPrice + (coin.CurrentPrice * 0.0045))
                await PlaceOrder("BTC", "USDT", coin.CurrentPrice, false, "MARKET", 0.0026m, coin);

        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message.ToString());
        }
        finally
        {
            isRunning = false;
        }
    }

    private static async Task PlaceOrder(string primaryCoin, string secondaryCoin, float currentPrice, bool isSellOrder, string orderType, decimal quantity, Coin lastProcess)
    {
        try
        {
            string symbol = primaryCoin + secondaryCoin;
            Side side = new();

            if (isSellOrder)
            {
                side = Side.SELL;
            }
            else
            {
                side = Side.BUY;
            }

            OrderType order = new();

            if (orderType == "MARKET")
                order = OrderType.MARKET;
            else
                order = OrderType.LIMIT;

            // Binance API endpoint
            string baseUrl = "https://api.binance.com";
            string endpoint = "/api/v3/order";


            // Send request to Binance API
            using (var httpClient = new HttpClient())
            {

                var market = new Market(httpClient);

                SpotAccountTrade spot = new SpotAccountTrade(httpClient, new BinanceHmac(await Coin.getApis()), apiKey: await Coin.getApi());

                long timeDifference = await CalculateRecWindow();

                string result = await spot.NewOrder(symbol, side, order, null, quantity, null, null, null, null, null, null, null, null, null, timeDifference);
               
                if (!string.IsNullOrEmpty(result))
                {
                    if (isSellOrder)
                    {
                        lastProcess.LastSoldPrice = currentPrice;
                        lastProcess.LastProcessIsSell = true;
                    }
                    else
                    {
                        lastProcess.LastBoughtPrice = currentPrice;
                        lastProcess.LastProcessIsSell = false;
                    }

                    lastProcess.ProcessTime = DateTime.Now;
                    lastProcess.PrimaryCoin = primaryCoin; lastProcess.SecondaryCoin = secondaryCoin;
                    lastProcess.CoinName = primaryCoin;

                    Variables.Result = await Coin.InsertLastProcess(lastProcess);
                    if (Variables.Result && isSellOrder)
                        Console.WriteLine($"Sold {primaryCoin} at {currentPrice} at " + DateTime.Now + " and recorded");
                    else if (Variables.Result && !isSellOrder)
                        Console.WriteLine($"Bought {primaryCoin} at {currentPrice} at " + DateTime.Now + " and recorded");
                    else if (!Variables.Result && isSellOrder)
                        Console.WriteLine($"Sold {primaryCoin} at {currentPrice} at " + DateTime.Now + " and couldn't recorded");
                    else if (!Variables.Result && !isSellOrder)
                        Console.WriteLine($"Bought {primaryCoin} at {currentPrice} at " + DateTime.Now + " and couldn't recorded");
                }

            }

        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message.ToString();
            Console.WriteLine($"Error placing order: {errorMessage}" + DateTime.Now);

        }


    }
    static async Task<long> CalculateRecWindow()
    {
        try
        {
            string baseUrl = "https://api.binance.com";
            string endpoint = "/api/v3/time";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(baseUrl);

                var response = await httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                // Get the server time provided by Binance
                long serverTime = jsonResponse.serverTime;

                // Get the current local time
                long localTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Calculate the time difference between server and local time
                long offset = serverTime - localTime;
                long correctedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + offset;
                long recvWindow = Math.Max(5000, Math.Abs(offset)); // extra buffer

                // Adjust the recvWindow based on the time difference



                return recvWindow;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server time: {ex.Message}");
            throw;
        }
    }
}
