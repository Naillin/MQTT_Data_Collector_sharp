using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using MQTT_Data_Сollector_sharp.Core.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MQTT_Data_Сollector_sharp.Core
{
	internal class HttpAPIClient : IHttpAPIClient
	{
		private readonly HttpClientHandler _handler = new HttpClientHandler
		{
			UseCookies = true,
		};
		private readonly HttpClient _httpClient;

		private readonly string _baseUrl;
		private readonly string _login;
		private readonly string _password;
		private readonly ILogger _logger;

		public HttpAPIClient(string url, string login, string password, ILogger logger)
		{
			_baseUrl = url;
			_login = login;
			_password = password;
			_logger = logger;

			_httpClient = new HttpClient(_handler);
		}

		public async Task<bool> CheckAuthAsync()
		{
			var response = await _httpClient.GetAsync($"{_baseUrl}/api/check-auth");
			return response.IsSuccessStatusCode; // 200 = авторизован, 401 = нет
		}

		public async Task<(bool Success, int? UserId, string Error)> LoginAsync()
		{
			var content = new StringContent(
				JsonSerializer.Serialize(new { login_user = _login, password_user = _password }),
				Encoding.UTF8,
				"application/json");

			var response = await _httpClient.PostAsync($"{_baseUrl}/api/login", content);

			if (response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<LoginResponse>(responseContent);
				return (true, result?.user_id, string.Empty);
			}
			else
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				return (false, null, errorContent);
			}
		}

		public async Task<bool> LogoutAsync()
		{
			var response = await _httpClient.GetAsync($"{_baseUrl}/api/logout");
			return response.IsSuccessStatusCode;
		}

		public async Task<APIQueryResult> ExecuteQueryInternalAsync(string sql, object[]? parameters)
		{
			if (!await CheckAuthAsync())
				await LoginAsync();

			var requestData = new
			{
				sql,
				args = parameters ?? Array.Empty<object>()
			};

			var content = new StringContent(
				JsonSerializer.Serialize(requestData),
				Encoding.UTF8,
				"application/json");

			var response = await _httpClient.PostAsync($"{_baseUrl}/api/execute-query", content);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"API error: {errorContent}");
			}

			var responseContent = await response.Content.ReadAsStringAsync();
			var result = JsonSerializer.Deserialize<APIQueryResult>(responseContent);

			if (result == null)
				throw new Exception("Invalid API response");

			return result;
		}
	}
}
