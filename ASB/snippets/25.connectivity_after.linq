<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	const string queueName = "some-queue";
	const int NumberOfConnections = 50;

	await EnsureQueueExists(queueName, connectionString);

	var factories = new MessagingFactory[NumberOfConnections];
	var clients = new QueueClient[NumberOfConnections];
	for (int i = 0; i < NumberOfConnections; i++)
	{
		factories[i] = MessagingFactory.CreateFromConnectionString(connectionString);
		clients[i] = factories[i].CreateQueueClient("some-queue");
	}
	
	"Press any key to stop".Dump();
	Console.ReadLine();
	
	for (int i = 0; i < NumberOfConnections; i++)
	{
		clients[i].Close();
		factories[i].Close();
	}
}

static async Task EnsureQueueExists(string queueName, string connectionString)
{
	var namespaceNamanager = NamespaceManager.CreateFromConnectionString(connectionString);

	if (!await namespaceNamanager.QueueExistsAsync(queueName))
		await namespaceNamanager.CreateQueueAsync(queueName);
}