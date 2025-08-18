
namespace MQTT_Data_Сollector_sharp.Core.Entities
{
	internal class Topic
	{
		public int ID_Topic { get; set; }

		public string? Name_Topic { get; set; }

		public string? Path_Topic { get; set; }

		public double Latitude_Topic { get; set; }

		public double Longitude_Topic { get; set; }

		public double Altitude_Topic { get; set; }

		public double AltitudeSensor_Topic { get; set; }

		public long CheckTime_Topic { get; set; }

		//-------------------------------------------------------------

		public ICollection<Data> Data { get; set; }

		public ICollection<AreaPoint> AreaPoints { get; set; }

		public Topic()
		{
			Data = new List<Data>();
			AreaPoints = new List<AreaPoint>();
		}
	}
}
