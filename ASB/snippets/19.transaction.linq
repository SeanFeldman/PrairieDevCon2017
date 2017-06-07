<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
	//Process.Start(@"C:\Users\Sean\OneDrive\Tools\ServiceBusExplorer\ServiceBusExplorer.exe");
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	await CreateEntities(connectionString, "queue", 1);

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
	var messageSender = await messagingFactory.CreateMessageSenderAsync("queue1");

	using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
	{
		var sends = new List<Task>();
		for (int i = 1; i <= 10; i++)
		{
			sends.Add(messageSender.SendAsync(new BrokeredMessage("message #" + i)));
		}
		
		// messages are "staged"
		await Task.WhenAll(sends);
		await ReportNumberOfMessages(connectionString, "queue1");
		
		// messages are forwarded on the broker
		scope.Complete();
	}
	
	"Messages dispatched".Dump();
	await ReportNumberOfMessages(connectionString, "queue1");
}

static async Task ReportNumberOfMessages(string connectionString, string queuePath)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	var queueDescription = await namespaceManager.GetQueueAsync(queuePath);
	queueDescription.MessageCountDetails.ActiveMessageCount.Dump("Messages in queue");
}

static async Task CreateEntities(string connectionString, string queuePrefixPath, int numberOfQueues)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	for (int i = numberOfQueues; i > 0; i--)
	{
		var queuePath = queuePrefixPath + i;
		var queueDescription = new QueueDescription(queuePath);
		await EnsureQueueDoesNotExist(namespaceManager, queuePath);
		await namespaceManager.CreateQueueAsync(queueDescription);
	}

	$"Queues created".Dump();
}

static async Task EnsureQueueDoesNotExist(NamespaceManager namespaceManager, string queuePath)
{
	if (await namespaceManager.QueueExistsAsync(queuePath))
	{
		await namespaceManager.DeleteTopicAsync(queuePath);
	}
}