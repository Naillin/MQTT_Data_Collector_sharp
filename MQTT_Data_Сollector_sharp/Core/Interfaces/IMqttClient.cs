using MQTT_Data_Сollector_sharp.Core.Models;

namespace MQTT_Data_Сollector_sharp.Core.Interfaces
{
	internal interface IMqttClient
	{
		Task Connect();

		Task Disconnect();

		Task Reconnect();

		bool IsConnected { get; }

		Task SubscribeAsync(string topic);

		Task SubscribeAsync(string[] topics);

		Task UnsubscribeAsync(string topic);

		Task UnsubscribeAsync(string[] topics);

		Task UnsubscribeAllAsync();

		IReadOnlyCollection<string> GetSubscriptions();

		Task Publish(string topic, string payload);

		event EventHandler<MqttMessageReceivedEventArgs> MessageReceived;
	}
}
