
using System.Net.Http;
using Grpc.Core;
using System.Threading.Tasks;
using System.Text.Json;
using GrpcService.Server.Contracts;

namespace GrpcService.Server.Services
{
    public class WeatherService : Server.WeatherService.WeatherServiceBase
    {

        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
                
        public override async Task<WeatherResponse> GetCurrentWeather(
            GetCurrentWeatherForCityRequest request, 
            ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var responseText = await httpClient.GetStringAsync(
                $"https://api.openweathermap.org/data/2.5/weather?q={request.City}&appid=8249b2ce8322fa7479c5f5e89a32eb71&units={request.Units}"
            );

            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText);

            return new WeatherResponse
            {
                Temperature = temperatures!.Main.Temp,
                FeelsLike = temperatures.Main.FeelsLike,
                Lat = temperatures.Coord.Lat,
                Lon = temperatures.Coord.Lon
            };
        }
    }
}
