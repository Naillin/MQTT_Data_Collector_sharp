using Microsoft.EntityFrameworkCore;
using MQTT_Data_Сollector_sharp.Core.Entities;
using MQTT_Data_Сollector_sharp.Core.Interfaces;

namespace MQTT_Data_Сollector_sharp.DataWork.Repositories
{
	internal class DataRepository : IDataRepository
	{
		private readonly IDbContextFactory<AppDbContext> _factory;

		public DataRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

		public async Task SaveDataAsync(Data data)
		{
			await using var db = await _factory.CreateDbContextAsync();
			db.Data.Add(data);
			await db.SaveChangesAsync();
		}

		public async Task<int?> GetIdTopicAsync(string pathTopic)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics
				.Where(t => t.Path_Topic == pathTopic)
				.Select(t => (int?)t.ID_Topic)  // Приведение к nullable
				.FirstOrDefaultAsync(); ;
		}

		public async Task<List<Topic>> GetAllTopicsAsync()
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics.ToListAsync();
		}
	}
}
