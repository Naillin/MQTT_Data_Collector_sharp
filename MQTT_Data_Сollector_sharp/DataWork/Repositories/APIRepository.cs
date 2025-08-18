using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using System.Threading;

namespace MQTT_Data_Сollector_sharp.DataWork.Repositories
{
	internal class APIRepository : IDataRepository
	{
		private readonly IHttpAPIClient _httpAPIClient;

		public APIRepository(IHttpAPIClient httpAPIClient) => _httpAPIClient = httpAPIClient;

		public async Task SaveDataAsync(Data data)
		{
			string sql = $"INSERT INTO Data ({data.ID_Topic}, {data.Value_Data ?? (object)DBNull.Value}, {data.Time_Data}) VALUES (@idTopic, @value, @time)";

			var result = await _httpAPIClient.ExecuteQueryInternalAsync(sql, null);

			// Проверяем успешность операции
			if (result.Type != "modify" || result.RowCount <= 0)
			{
				throw new Exception("Failed to save data to database.");
			}
		}

		public async Task<int?> GetIdTopicAsync(string pathTopic)
		{
			string sql = $"SELECT ID_Topic FROM Topics WHERE Path_Topic = '{pathTopic}' LIMIT 1";

			var result = await _httpAPIClient.ExecuteQueryInternalAsync(sql, null);

			// Проверяем что это SELECT ответ и есть данные
			if (result.Type != "select" || result.RawData == null || result.RawData.Count == 0)
				return null;

			// Берем первый элемент первой строки (ID_Topic)
			var idValue = result.Data?[0]?[0];

			return idValue switch
			{
				long l => (int)l,
				int i => i,
				_ => null
			};
		}

		public async Task<List<Topic>> GetAllTopicsAsync()
		{
			const string sql = "SELECT ID_Topic, Name_Topic, Path_Topic, Latitude_Topic, Longitude_Topic, Altitude_Topic, AltitudeSensor_Topic, CheckTime_Topic FROM Topics";

			var result = await _httpAPIClient.ExecuteQueryInternalAsync(sql, null);

			if (result.Type != "select" || result.RawData == null || result.Columns == null)
				return new List<Topic>();

			var topics = new List<Topic>();

			// Создаем маппинг индексов колонок
			var columnIndices = new Dictionary<string, int>();
			for (int i = 0; i < result.Columns.Count; i++)
			{
				columnIndices[result.Columns[i]] = i;
			}

			foreach (var row in result.Data ?? Enumerable.Empty<List<object?>>())
			{
				var topic = new Topic();

				// Маппинг полей с проверкой наличия колонок в результате
				if (columnIndices.TryGetValue("ID_Topic", out var idx) && row[idx] is long id)
					topic.ID_Topic = (int)id;

				if (columnIndices.TryGetValue("Name_Topic", out idx))
					topic.Name_Topic = row[idx]?.ToString();

				if (columnIndices.TryGetValue("Path_Topic", out idx))
					topic.Path_Topic = row[idx]?.ToString();

				if (columnIndices.TryGetValue("Latitude_Topic", out idx) && row[idx] is double lat)
					topic.Latitude_Topic = lat;

				if (columnIndices.TryGetValue("Longitude_Topic", out idx) && row[idx] is double lon)
					topic.Longitude_Topic = lon;

				if (columnIndices.TryGetValue("Altitude_Topic", out idx) && row[idx] is double alt)
					topic.Altitude_Topic = alt;

				if (columnIndices.TryGetValue("AltitudeSensor_Topic", out idx) && row[idx] is double altSens)
					topic.AltitudeSensor_Topic = altSens;

				if (columnIndices.TryGetValue("CheckTime_Topic", out idx) && row[idx] is long time)
					topic.CheckTime_Topic = time;

				topics.Add(topic);
			}

			return topics;
		}
	}
}
