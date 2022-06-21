
using System.Net.Http;
using Grpc.Core;
using System.Threading.Tasks;
using System.Text.Json;
using GrpcService.Server.Contracts;
using Google.Protobuf.WellKnownTypes;
using System;
using Microsoft.Extensions.Logging;

namespace GrpcService.Server.Services
{
    public class WeatherService : Server.WeatherService.WeatherServiceBase
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
                
        public override async Task<WeatherResponse> GetCurrentWeather(
            GetCurrentWeatherForCityRequest request, 
            ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Temperatures temperatures = await GetCurrentTemperaturesAsync(request, httpClient);

            return new WeatherResponse
            {
                Temperature = temperatures!.Main.Temp,
                FeelsLike = temperatures.Main.FeelsLike,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                Lat = temperatures.Coord.Lat,
                Lon = temperatures.Coord.Lon,
            };
        }

       
        private async Task<Temperatures> GetCurrentTemperaturesAsync(GetCurrentWeatherForCityRequest request, HttpClient httpClient)
        {
            string APIKEY = "8249b2ce8322fa7479c5f5e89a32eb71";

            var responseText = await httpClient.GetStringAsync(
                $"https://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={APIKEY}&units={request.Units}"
            );

            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText);
            return temperatures;
        }

        public override async Task GetCurrentWeatherStream(
           GetCurrentWeatherForCityRequest request,
           IServerStreamWriter<WeatherResponse> responseStream,
           ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();

            for (var i = 0; i < 30; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Request was cancelled!");
                    break;
                }

                var temperatures = await GetCurrentTemperaturesAsync(request, httpClient);
                await responseStream.WriteAsync(new WeatherResponse
                {
                    Temperature = temperatures!.Main.Temp,
                    FeelsLike = temperatures.Main.FeelsLike,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    Lat = temperatures.Coord.Lat,
                    Lon = temperatures.Coord.Lon,
                    Iter = i,
                }); ;
                await Task.Delay(1000);
            }

        }
    }
}
