using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using MQTT_Data_Сollector_sharp.Core.Models;
using MQTT_Data_Сollector_sharp.Workers;
using System.Collections.Concurrent;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTT_Data_Сollector_sharp.Core
{
	internal class M2MqttClient : IMqttClient
	{
		private bool _IsConnected = false;
		public bool IsConnected => _IsConnected;
		public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

		private string _clientId;
		private string _username;
		private string _password;

		//private readonly ILogger<M2MqttClient> _logger;
		private readonly LoggerManager loggerM2MqttClient;

		private MqttClient _mqttClient;
		private readonly HashSet<string> _subscriptions = new();

		public M2MqttClient(string brokerAddress, int port, string username, string password, ILogger<M2MqttClient> logger)
		{
			_clientId = Guid.NewGuid().ToString();
			_username = username;
			_password = password;
			//_logger = logger;
			loggerM2MqttClient = new LoggerManager(logger, "M2MqttClient");

			// Создание клиента MQTT
			_mqttClient = new MqttClient(brokerAddress, port, false, null, null, MqttSslProtocols.None);

			// Настройка обработчиков событий
			_mqttClient.MqttMsgPublishReceived += OnMqttMsgPublishReceived;
			_mqttClient.ConnectionClosed += OnConnectionClosed;
		}

		private bool disconnectMode = false;
		public Task Connect()
		{
			if (!_mqttClient.IsConnected)
			{
				//засунуть всю работу методов вawait Task.Run(() => ); и сделать все методы async !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

				try
				{
					loggerM2MqttClient.LogInformation("Connecting to MQTT broker...");
					_mqttClient.Connect(_clientId, _username, _password);
					loggerM2MqttClient.LogInformation("Connected to MQTT broker.");
				}
				catch (Exception ex)
				{
					loggerM2MqttClient.LogError($"Connection failed: {ex.Message}");
				}
			}

			return Task.CompletedTask;
		}

		public Task Disconnect()
		{
			if (_mqttClient.IsConnected)
			{
				loggerM2MqttClient.LogInformation("Disconnecting from MQTT broker...");
				disconnectMode = true;
				_mqttClient.Disconnect();
				loggerM2MqttClient.LogInformation("Disconnected from MQTT broker.");
			}

			return Task.CompletedTask;
		}

		public Task Reconnect()
		{
			while (!_mqttClient.IsConnected && !disconnectMode)
			{
				try
				{
					loggerM2MqttClient.LogInformation("Reconnecting to MQTT broker...");
					_mqttClient.Connect(_clientId);
					loggerM2MqttClient.LogInformation("Reconnected to MQTT broker.");
				}
				catch (Exception ex)
				{
					loggerM2MqttClient.LogError($"Reconnection failed: {ex.Message}");
					Task.Delay(5000);
				}
			}

			return Task.CompletedTask;
		}

		public Task SubscribeAsync(string topic)
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				loggerM2MqttClient.LogInformation($"Subscribing to topic: {topic}");
				_mqttClient.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
				_subscriptions.Add(topic);
				loggerM2MqttClient.LogInformation($"Subscribed to topic: {topic}");
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot subscribe.");
				Reconnect();
			}

			return Task.CompletedTask;
		}

		public Task SubscribeAsync(string[] topics)
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				byte[] qosLevels = new byte[topics.Length];
				for (int i = 0; i < topics.Length; i++)
				{
					qosLevels[i] = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
					_subscriptions.Add(topics[i]);
					loggerM2MqttClient.LogInformation($"Subscribing to topic: {topics[i]}");
				}

				_mqttClient.Subscribe(topics, qosLevels);
				loggerM2MqttClient.LogInformation("Subscribed to all topics.");
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot subscribe.");
				Reconnect();
			}

			return Task.CompletedTask;
		}

		public Task UnsubscribeAsync(string topic)
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				loggerM2MqttClient.LogInformation($"Unsubscribing from topic: {topic}");
				_subscriptions.Remove(topic);
				_mqttClient.Unsubscribe(new string[] { topic });
				loggerM2MqttClient.LogInformation($"Unsubscribed from topic: {topic}");
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot unsubscribe.");
				Reconnect();
			}

			return Task.CompletedTask;
		}

		public Task UnsubscribeAsync(string[] topics)
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				loggerM2MqttClient.LogInformation("Unsubscribing from topics:");
				foreach (var topic in topics)
				{
					loggerM2MqttClient.LogInformation($"- {topic}");
					_subscriptions.Remove(topic);
				}

				_mqttClient.Unsubscribe(topics);
				loggerM2MqttClient.LogInformation("Unsubscribed from all specified topics.");
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot unsubscribe.");
				Reconnect();
			}

			return Task.CompletedTask;
		}

		public Task UnsubscribeAllAsync()
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				_subscriptions.ToList().ForEach(s => loggerM2MqttClient.LogInformation($", {s}"));
				_mqttClient.Unsubscribe(_subscriptions.ToArray<string>());
				_subscriptions.Clear();
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot unsubscribe.");
				Reconnect();
			}
				

			return Task.CompletedTask;
		}

		public Task Publish(string topic, string payload)
		{
			if (!_mqttClient.IsConnected)
				Connect();

			if (_mqttClient.IsConnected)
			{
				_mqttClient.Publish(topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
				loggerM2MqttClient.LogInformation($"Message published to topic: {topic}");
			}
			else
			{
				loggerM2MqttClient.LogError("Client is not connected. Cannot publish.");
				Reconnect();
			}

			return Task.CompletedTask;
		}

		public IReadOnlyCollection<string> GetSubscriptions()
		{
			return _subscriptions.ToList().AsReadOnly();
		}

		// Обработчик события получения сообщения
		private void OnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			var payload = Encoding.UTF8.GetString(e.Message);
			loggerM2MqttClient.LogInformation($"Received message: {payload} from topic: {e.Topic}");

			// Вызов события для обработки сообщения
			MessageReceived?.Invoke(this, new MqttMessageReceivedEventArgs
			{
				Topic = e.Topic,
				Payload = payload
			});
		}

		// Обработчик события закрытия соединения
		private void OnConnectionClosed(object sender, EventArgs e)
		{
			loggerM2MqttClient.LogInformation("Disconnected from MQTT broker.");
			if (!_mqttClient.IsConnected)
			{
				Reconnect();
			}
		}
	}
}
