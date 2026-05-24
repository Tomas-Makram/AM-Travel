using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BusinessLayer.Services
{
    public interface ITelegramService
    {
        Task<bool> SendMessageAsync(string chatId, string message);
    }

    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly TelegramSettings _settings;

        public TelegramService(HttpClient httpClient, IOptions<TelegramSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<bool> SendMessageAsync(string chatId, string message)
        {
            var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";

            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                chat_id = chatId,
                text = message
            });

            return response.IsSuccessStatusCode;
        }
    }
}