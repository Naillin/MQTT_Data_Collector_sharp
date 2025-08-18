using MQTT_Data_Сollector_sharp.Core.Entities;

namespace MQTT_Data_Сollector_sharp.Core.Interfaces
{
	internal interface IDataRepository
	{
		Task SaveDataAsync(Data data);

		Task<int?> GetIdTopicAsync(string pathTopic);

		Task<List<Topic>> GetAllTopicsAsync();
	}
}
