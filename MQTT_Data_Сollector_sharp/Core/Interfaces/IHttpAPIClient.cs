using MQTT_Data_Сollector_sharp.Core.Models;

namespace MQTT_Data_Сollector_sharp.Core.Interfaces
{
	internal interface IHttpAPIClient
	{
		Task<bool> CheckAuthAsync();

		Task<(bool Success, int? UserId, string Error)> LoginAsync();

		Task<bool> LogoutAsync();

		Task<APIQueryResult> ExecuteQueryInternalAsync(string sql, object[]? parameters);
	}
}
