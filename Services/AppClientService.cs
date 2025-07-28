using ADUSAdm.Shared;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace ADUSAdm.Services
{
    public class AppClientService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;

        public AppClientService(HttpClient httpClient, IOptions<AppSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;

            _httpClient.BaseAddress = new Uri(_settings.urlapi);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.JwtToken);
        }

        public async Task<string> GetDadosAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}