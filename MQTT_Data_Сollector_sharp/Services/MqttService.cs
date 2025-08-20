using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core;
using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;

namespace MQTT_Data_Сollector_sharp.Services
{
	internal class MqttService
	{
		private readonly IDataRepository _dataRepository;

		//private readonly ILogger<MqttService> _logger;
		private readonly LoggerManager loggerMqttService;
		public MqttService(IDataRepository dataRepository, ILogger<MqttService> logger)
		{
			_dataRepository = dataRepository;
			//_logger = logger;
			loggerMqttService = new LoggerManager(logger, "MqttService");
		}

		public async Task SaveDataAsync(string topic, string value)
		{
			loggerMqttService.LogInformation($"Putting data in database.");

			int id = Convert.ToInt32(await _dataRepository.GetIdTopicAsync(topic));
			long time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			Data data = new Data { ID_Topic = id, Value_Data = value, Time_Data = time };
			await _dataRepository.SaveDataAsync(data);

			loggerMqttService.LogInformation($"Putted value {value} at {time.ToString()} time.");
		}
	}
}
