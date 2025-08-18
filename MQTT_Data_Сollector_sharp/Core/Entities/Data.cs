
namespace MQTT_Data_Сollector_sharp.Core.Entities
{
	internal class Data
	{
		public int ID_Data { get; set; }

		public int ID_Topic { get; set; }

		public string? Value_Data { get; set; }

		public long Time_Data { get; set; }

		//-------------------------------------------------------------

		public Topic? Topic { get; set; }
	}
}
