<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
	Process.Start(@"C:\Users\Sean\OneDrive\Tools\ServiceBusExplorer\ServiceBusExplorer.exe");
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	await CreateEntities(connectionString, "queue", 5);

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);

	var messageSender1 = await messagingFactory.CreateMessageSenderAsync("queue1");
	await messageSender1.SendAsync(new BrokeredMessage("first message"));

	var messageSender2 = await messagingFactory.CreateMessageSenderAsync("queue2");
	await messageSender2.SendAsync(new BrokeredMessage("second message"));
	
	"Messages dispatched.\nWhat do you expect to see?".Dump();
}


static async Task CreateEntities(string connectionString, string queuePrefixPath, int numberOfQueues)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	for (int i = numberOfQueues; i > 0; i--)
	{
		var queuePath = queuePrefixPath + i;
		var queueDescription = new QueueDescription(queuePath);
		if (i < numberOfQueues)
		{
			queueDescription.ForwardTo = queuePrefixPath + (i + 1);
			$"{queuePath} --> {queueDescription.ForwardTo}".Dump();
		}
		await EnsureQueueDoesNotExist(namespaceManager, queuePath);
		await namespaceManager.CreateQueueAsync(queueDescription);
	}

	$"Queues created\n".Dump();
}

static async Task EnsureQueueDoesNotExist(NamespaceManager ns, string queuePath)
{
	if (await ns.QueueExistsAsync(queuePath))
	{
		await ns.DeleteTopicAsync(queuePath);
	}
}