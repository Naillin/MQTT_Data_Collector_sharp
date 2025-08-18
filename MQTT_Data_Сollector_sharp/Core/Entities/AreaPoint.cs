
namespace MQTT_Data_Сollector_sharp.Core.Entities
{
	internal class AreaPoint
	{
		public int ID_AreaPoint { get; set; }

		public int ID_Topic { get; set; }

		public string? Depression_AreaPoint { get; set; }

		public string? Perimeter_AreaPoint { get; set; }

		public string? Included_AreaPoint { get; set; }

		public string? Islands_AreaPoint { get; set; }

		//-------------------------------------------------------------

		public Topic? Topic { get; set; }
	}
}
