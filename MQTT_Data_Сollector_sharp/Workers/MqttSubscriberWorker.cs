using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using System.IO;

namespace MQTT_Data_Сollector_sharp.Workers
{
	internal class MqttSubscriberWorker : IDisposable
	{
		private readonly IMqttClient _mqttClient;
		private readonly IDataRepository _dataRepository;
		private readonly ILogger _logger;

		private CancellationTokenSource? _cts;
		private Task? _runningTask;

		public MqttSubscriberWorker(IMqttClient mqttClient, IDataRepository dataRepository, ILogger logger)
		{
			_mqttClient = mqttClient;
			_dataRepository = dataRepository;
			_logger = logger;
		}

		public Task StartAsync()
		{
			_logger.LogInformation("Running worker.");
			_cts = new CancellationTokenSource();
			_runningTask = RunAsync(_cts.Token);

			return _runningTask;
		}

		public async Task StopAsync()
		{
			_logger.LogInformation("Stoping worker.");
			_cts?.Cancel();
			if (_runningTask != null)
				await _runningTask;
		}

		private async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					var topics = await _dataRepository.GetAllTopicsAsync();
					var currentSubscriptions = _mqttClient.GetSubscriptions();
					// Фильтрация null и выборка путей
					var validPaths = topics
						.Where(t => t.Path_Topic != null && !currentSubscriptions.Contains(t.Path_Topic))
						.Select(t => t.Path_Topic!)
						.Distinct()  // Убираем дубликаты
						.ToArray();

					if (validPaths.Length > 0)
					{
						foreach (var path in validPaths)
						{
							_logger.LogInformation($"Subscribing to: {path}");
						}

						await _mqttClient.SubscribeAsync(validPaths);
					}

					await Task.Delay(5000, token); // Пауза между проверками
				}
				catch (TaskCanceledException) { /* Выход по Cancel */ }
				catch (Exception ex)
				{
					_logger.LogError(ex, "Subscriber error");
					await Task.Delay(1000, token); // Пауза после ошибки
				}
			}
		}

		public void Dispose() => _cts?.Dispose();
	}
}
