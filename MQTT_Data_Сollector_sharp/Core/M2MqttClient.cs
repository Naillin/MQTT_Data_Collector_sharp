using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using MQTT_Data_Сollector_sharp.Core.Models;
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

		private readonly ILogger _logger;

		private MqttClient _mqttClient;
		private readonly HashSet<string> _subscriptions = new();

		public M2MqttClient(string brokerAddress, int port, string username, string password, ILogger logger)
		{
			_clientId = Guid.NewGuid().ToString();
			_username = username;
			_password = password;
			_logger = logger;

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
					_logger.LogInformation("Connecting to MQTT broker...");
					_mqttClient.Connect(_clientId, _username, _password);
					_logger.LogInformation("Connected to MQTT broker.");
				}
				catch (Exception ex)
				{
					_logger.LogError($"Connection failed: {ex.Message}");
				}
			}

			return Task.CompletedTask;
		}

		public Task Disconnect()
		{
			if (_mqttClient.IsConnected)
			{
				_logger.LogInformation("Disconnecting from MQTT broker...");
				disconnectMode = true;
				_mqttClient.Disconnect();
				_logger.LogInformation("Disconnected from MQTT broker.");
			}

			return Task.CompletedTask;
		}

		public Task Reconnect()
		{
			while (!_mqttClient.IsConnected && !disconnectMode)
			{
				try
				{
					_logger.LogInformation("Reconnecting to MQTT broker...");
					_mqttClient.Connect(_clientId);
					_logger.LogInformation("Reconnected to MQTT broker.");
				}
				catch (Exception ex)
				{
					_logger.LogError($"Reconnection failed: {ex.Message}");
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
				_logger.LogInformation($"Subscribing to topic: {topic}");
				_mqttClient.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
				_subscriptions.Add(topic);
				_logger.LogInformation($"Subscribed to topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot subscribe.");
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
					_logger.LogInformation($"Subscribing to topic: {topics[i]}");
				}

				_mqttClient.Subscribe(topics, qosLevels);
				_logger.LogInformation("Subscribed to all topics.");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot subscribe.");
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
				_logger.LogInformation($"Unsubscribing from topic: {topic}");
				_subscriptions.Remove(topic);
				_mqttClient.Unsubscribe(new string[] { topic });
				_logger.LogInformation($"Unsubscribed from topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
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
				_logger.LogInformation("Unsubscribing from topics:");
				foreach (var topic in topics)
				{
					_logger.LogInformation($"- {topic}");
					_subscriptions.Remove(topic);
				}

				_mqttClient.Unsubscribe(topics);
				_logger.LogInformation("Unsubscribed from all specified topics.");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
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
				_subscriptions.ToList().ForEach(s => _logger.LogInformation($", {s}"));
				_mqttClient.Unsubscribe(_subscriptions.ToArray<string>());
				_subscriptions.Clear();
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
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
				_logger.LogInformation($"Message published to topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot publish.");
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
			_logger.LogInformation($"Received message: {payload} from topic: {e.Topic}");

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
			_logger.LogInformation("Disconnected from MQTT broker.");
			if (!_mqttClient.IsConnected)
			{
				Reconnect();
			}
		}
	}
}
