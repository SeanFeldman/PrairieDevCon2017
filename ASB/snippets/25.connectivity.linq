<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	Process.Start("perfmon.msc");
	Process.Start(@"C:\Users\Sean\OneDrive\Documents\Presintations\Prairie DevCon 2017\ASB\snippets\25.connectivity_after.linq");
	MainAsync().GetAwaiter().GetResult();
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");
	
	const string queueName = "some-queue";
	const int NumberOfConnections = 50;
	
	await EnsureQueueExists(queueName, connectionString);

	var factory =  MessagingFactory.CreateFromConnectionString(connectionString);
	var clients = new QueueClient[NumberOfConnections];
	var sends = new List<Task>();
	for (int i = 0; i < NumberOfConnections; i++)
	{
		clients[i] = factory.CreateQueueClient(queueName);
		sends.Add(clients[i].SendAsync(new BrokeredMessage()));
	}
	
	await Task.WhenAll(sends);
	
	"Press any key to stop".Dump();
	Console.ReadLine();
	
	for (int i = 0; i < NumberOfConnections; i++)
	{
		await clients[i].CloseAsync();
	}
	await factory.CloseAsync();
}

static async Task EnsureQueueExists(string queueName, string connectionString) 
{
	var namespaceNamanager = NamespaceManager.CreateFromConnectionString(connectionString);
		
	if (!await namespaceNamanager.QueueExistsAsync(queueName))
		await namespaceNamanager.CreateQueueAsync(queueName);
}