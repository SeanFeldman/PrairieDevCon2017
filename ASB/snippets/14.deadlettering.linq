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
	Process.Start(@"C:\Users\Sean\OneDrive\Documents\Presintations\Prairie DevCon 2017\ASB\snippets\14.deadlettering_ISSUE.linq");
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	await CreateEntities(connectionString, "queue", 5, "dlq");

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);

	var messageSender1 = await messagingFactory.CreateMessageSenderAsync("queue1");
	await messageSender1.SendAsync(new BrokeredMessage("first message"));

	var messageSender2 = await messagingFactory.CreateMessageSenderAsync("queue2");
	await messageSender2.SendAsync(new BrokeredMessage("second message"));
	
	"Messages dispatched".Dump();
	
	var messageReceiver1 = await messagingFactory.CreateMessageReceiverAsync("queue1");
	await messageReceiver1.ReceiveAsync();

	var messageReceiver2 = await messagingFactory.CreateMessageReceiverAsync("queue2");
	await messageReceiver2.ReceiveAsync();
	
	"Messages received and dead-lettered".Dump();
}


static async Task CreateEntities(string connectionString, string queuePrefixPath, int numberOfQueues, string centralizedDlqPath)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

	await EnsureQueueDoesNotExist(namespaceManager, centralizedDlqPath);
	await namespaceManager.CreateQueueAsync(centralizedDlqPath);
	
	for (int i = numberOfQueues; i > 0; i--)
	{
		var queuePath = queuePrefixPath + i;
		var queueDescription = new QueueDescription(queuePath)
		{
			ForwardDeadLetteredMessagesTo = centralizedDlqPath,
			MaxDeliveryCount = 1,
			LockDuration = TimeSpan.FromSeconds(1)
		};
		
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