
namespace MQTT_Data_Сollector_sharp.Core.Models
{
	internal class MqttMessageReceivedEventArgs : EventArgs
	{
		public string? Topic { get; set; }
		public string? Payload { get; set; }
	}
}
