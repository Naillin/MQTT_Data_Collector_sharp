using IniParser;
using IniParser.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp.Core;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using MQTT_Data_Сollector_sharp.DataWork;
using MQTT_Data_Сollector_sharp.DataWork.Repositories;
using MQTT_Data_Сollector_sharp.Services;
using MQTT_Data_Сollector_sharp.Workers;
using System.Runtime.InteropServices;

namespace MQTT_Data_Сollector_sharp
{
	internal class Program
	{
		private static string MQTT_ADDRESS = "127.0.0.1";
		private static int MQTT_PORT = 3121;
		private static string MQTT_USERNAME = string.Empty;
		private static string MQTT_PASSWORD = string.Empty;

		private static int CONNECTION_METHOD = 0;

		private static string API_URL = "http://127.0.0.1:8080";
		private static string API_LOGIN = string.Empty;
		private static string API_PASSWORD = string.Empty;

		private const string filePathConfig = "config.ini";
		private static string configTextDefault = string.Empty;
		private static void InitConfig()
		{
			FileIniDataParser parser = new FileIniDataParser();

			if (File.Exists(filePathConfig))
			{
				IniData data = parser.ReadFile(filePathConfig);

				string[] linesConfig = File.ReadAllLines(filePathConfig);
				CONNECTION_METHOD = Convert.ToInt32(data["Settings"]["CONNECTION_METHOD"]);

				MQTT_ADDRESS = data["MQTT Settings"]["MQTT_ADDRESS"];
				MQTT_PORT = Convert.ToInt32(data["MQTT Settings"]["MQTT_PORT"]);
				MQTT_USERNAME = data["MQTT Settings"]["MQTT_USERNAME"];
				MQTT_PASSWORD = data["MQTT Settings"]["MQTT_PASSWORD"];

				API_URL = data["API Settings"]["API_URL"];
				API_LOGIN = data["API Settings"]["API_LOGIN"];
				API_PASSWORD = data["API Settings"]["API_PASSWORD"];
			}
			else
			{
				IniData data = new IniData();
				data.Sections.AddSection("Settings");
				data["Settings"]["CONNECTION_METHOD"] = CONNECTION_METHOD.ToString();

				data.Sections.AddSection("MQTT Settings");
				data["MQTT Settings"]["MQTT_ADDRESS"] = MQTT_ADDRESS;
				data["MQTT Settings"]["MQTT_PORT"] = MQTT_PORT.ToString();
				data["MQTT Settings"]["MQTT_USERNAME"] = MQTT_USERNAME;
				data["MQTT Settings"]["MQTT_PASSWORD"] = MQTT_PASSWORD;

				data.Sections.AddSection("API Settings");
				data["API Settings"]["API_URL"] = API_URL;
				data["API Settings"]["API_LOGIN"] = API_LOGIN;
				data["API Settings"]["API_PASSWORD"] = API_PASSWORD;

				parser.WriteFile(filePathConfig, data);
			}

			configTextDefault = $"CONNECTION_METHOD = [{CONNECTION_METHOD.ToString()}]\r\n" +

								$"MQTT_ADDRESS = [{MQTT_ADDRESS}]\r\n" +
								$"MQTT_PORT = [{MQTT_PORT.ToString()}]\r\n" +
								$"MQTT_USERNAME = [{MQTT_USERNAME}]\r\n" +
								$"MQTT_PASSWORD = [{MQTT_PASSWORD}]\r\n" +

								$"API_URL = [{API_URL}]\r\n" +
								$"API_LOGIN = [{API_LOGIN}]\r\n" +
								$"API_PASSWORD = [{API_PASSWORD}]";
		}

		static async Task Main(string[] args)
		{
			InitConfig();

			var serviceCollection = new ServiceCollection();

			// Logger
			serviceCollection.AddLogging(builder =>
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					builder.AddConsole();
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					builder.AddSystemdConsole();
			});

			// EF
			serviceCollection.AddDbContextFactory<AppDbContext>();

			// HTTP API
			serviceCollection.AddScoped<IHttpAPIClient>(sp => new HttpAPIClient(API_URL, API_LOGIN, API_PASSWORD, sp.GetRequiredService<ILogger<HttpAPIClient>>()));

			// IDataRepository выбор через CONNECTION_METHOD
			serviceCollection.AddScoped<IDataRepository>(sp =>
				CONNECTION_METHOD switch
				{
					0 => new DataRepository(sp.GetRequiredService<IDbContextFactory<AppDbContext>>()),
					1 => new APIRepository(sp.GetRequiredService<IHttpAPIClient>()),
					_ => throw new NotImplementedException()
				});

			// Сервисы
			serviceCollection.AddScoped<IMqttClient>(sp => new M2MqttClient(MQTT_ADDRESS, MQTT_PORT, MQTT_USERNAME, MQTT_PASSWORD, sp.GetRequiredService<ILogger<M2MqttClient>>()));
			serviceCollection.AddScoped<MqttService>();
			serviceCollection.AddScoped<MqttSubscriberWorker>();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			// Получаем зависимости через DI
			var loggerProgram = new LoggerManager(serviceProvider.GetRequiredService<ILogger<Program>>(), "Program");
			loggerProgram.LogInformation(configTextDefault);
			await Task.Delay(3000);

			var mqttClient = serviceProvider.GetRequiredService<IMqttClient>();
			var mqttService = serviceProvider.GetRequiredService<MqttService>();
			var mqttSubscriberWorker = serviceProvider.GetRequiredService<MqttSubscriberWorker>();

			// Подписка на события
			mqttClient.MessageReceived += async (senderMQTT, eMQTT) =>
			{
				try
				{
					loggerProgram.LogInformation($"Get data - Topic: [{eMQTT.Topic}] Message: [{eMQTT.Payload}]");

					if (!string.IsNullOrEmpty(eMQTT.Topic) && !string.IsNullOrEmpty(eMQTT.Payload))
					{
						await mqttService.SaveDataAsync(eMQTT.Topic, eMQTT.Payload);
					}
				}
				catch (Exception ex)
				{
					loggerProgram.LogError(ex, "Error in MQTT message receive!");
				}
			};

			await mqttSubscriberWorker.StartAsync();
		}
	}
}
