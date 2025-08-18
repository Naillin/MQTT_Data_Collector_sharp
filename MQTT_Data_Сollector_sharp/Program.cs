using IniParser;
using IniParser.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector_sharp;
using MQTT_Data_Сollector_sharp.Core;
using MQTT_Data_Сollector_sharp.Core.Interfaces;
using MQTT_Data_Сollector_sharp.DataWork;
using MQTT_Data_Сollector_sharp.DataWork.Repositories;
using MQTT_Data_Сollector_sharp.Services;
using MQTT_Data_Сollector_sharp.Workers;
using System.Net.Http;

class Program
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

		configTextDefault =	$"CONNECTION_METHOD = [{CONNECTION_METHOD.ToString()}]\r\n" +

							$"MQTT_ADDRESS = [{MQTT_ADDRESS}]\r\n" +
							$"MQTT_PORT = [{MQTT_PORT.ToString()}]\r\n" +
							$"MQTT_USERNAME = [{MQTT_USERNAME}]\r\n" +
							$"MQTT_PASSWORD = [{MQTT_PASSWORD}]\r\n" +
							
							$"API_URL = [{API_URL}]\r\n" +
							$"API_LOGIN = [{API_LOGIN}]\r\n" +
							$"API_PASSWORD = [{API_PASSWORD}]";
	}

	static async Task Main()
	{
		//Loggers
		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

		ILogger innerLoggerProgram = loggerFactory.CreateLogger<Program>();
		LoggerManager loggerProgram = new LoggerManager(innerLoggerProgram, "Program");

		ILogger innerLoggerMqttClient = loggerFactory.CreateLogger<M2MqttClient>();
		LoggerManager loggerMqttClient = new LoggerManager(innerLoggerMqttClient, "M2MqttClient");

		ILogger innerLoggerHttpAPIClient = loggerFactory.CreateLogger<HttpAPIClient>();
		LoggerManager loggerHttpAPIClient = new LoggerManager(innerLoggerHttpAPIClient, "HttpAPIClient");

		ILogger innerLoggerMqttService = loggerFactory.CreateLogger<MqttService>();
		LoggerManager loggerMqttService = new LoggerManager(innerLoggerMqttService, "MqttService");

		ILogger innerLoggerMqttSubscriberWorker = loggerFactory.CreateLogger<MqttSubscriberWorker>();
		LoggerManager loggerMqttSubscriberWorker = new LoggerManager(innerLoggerMqttSubscriberWorker, "MqttSubscriberWorker");

		InitConfig();
		loggerProgram.LogInformation(configTextDefault);
		await Task.Delay(3000);

		//Base
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddDbContextFactory<AppDbContext>();
		serviceCollection.AddScoped<IDataRepository, DataRepository>();
		ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
		
		IDbContextFactory<AppDbContext> dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

		//Connections
		IMqttClient mqttClient = new M2MqttClient(MQTT_ADDRESS, MQTT_PORT, MQTT_USERNAME, MQTT_PASSWORD, loggerMqttClient);
		IHttpAPIClient httpAPIClient = new HttpAPIClient(API_URL, API_LOGIN, API_PASSWORD, loggerHttpAPIClient);

		//Servises
		IDataRepository dataRepository = CONNECTION_METHOD switch
		{
			0 => new DataRepository(dbContextFactory),
			1 => new APIRepository(httpAPIClient),
			_ => throw new NotImplementedException()
		};
		MqttService mqttService = new MqttService(dataRepository, loggerMqttService);
		MqttSubscriberWorker mqttSubscriberWorker = new MqttSubscriberWorker(mqttClient, dataRepository, loggerMqttSubscriberWorker);

		//Run
		mqttClient.MessageReceived += async (senderMQTT, eMQTT) =>
		{
			try
			{
				loggerProgram.LogInformation($"Get data - Topic: [{eMQTT.Topic}] Message: [{eMQTT.Payload}]");

				if (eMQTT != null && !string.IsNullOrEmpty(eMQTT.Topic) && !string.IsNullOrEmpty(eMQTT.Payload))
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
