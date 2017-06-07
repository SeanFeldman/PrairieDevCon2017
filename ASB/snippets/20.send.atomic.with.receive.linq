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
	Process.Start(@"C:\Users\Sean\OneDrive\Documents\Presintations\Prairie DevCon 2017\ASB\snippets\20.send.atomic.with.receive_ISSUE.linq");
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	var queues = new[] { "source", "dest-a", "dest-b", "dest-c" };
	await CreateEntities(connectionString, queues);

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
	var messageSenderSource = await messagingFactory.CreateMessageSenderAsync("source"); 
	var messageReceiverA = await messagingFactory.CreateMessageReceiverAsync("source");
	await messageSenderSource.SendAsync(new BrokeredMessage("incoming message"));
	"Message sent to `source`".Dump();

	var messageSenderDestA = await messagingFactory.CreateMessageSenderAsync("dest-a", viaEntityPath: "source");
	var messageSenderDestB = await messagingFactory.CreateMessageSenderAsync("dest-B", viaEntityPath: "source");
	var messageSenderDestC = await messagingFactory.CreateMessageSenderAsync("dest-c", viaEntityPath: "source");
	
	var sends = new List<Task>();

	using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
	{
		var incomingMessage = await messageReceiverA.ReceiveAsync();
		
		await messageSenderDestA.SendAsync(new BrokeredMessage("outgoing to A"));
		await messageSenderDestB.SendAsync(new BrokeredMessage("outgoing to B"));
		await messageSenderDestC.SendAsync(new BrokeredMessage("outgoing to C"));
		
		// outgoing messages "staged"
		await Task.WhenAll(sends);
		await ReportNumberOfMessages(connectionString, queues);
		
		// incoming message completed
		//await incomingMessage.CompleteAsync();
		
		scope.Complete();
	}
	
	"Transaction completed".Dump();
	await ReportNumberOfMessages(connectionString, queues);
}

static async Task ReportNumberOfMessages(string connectionString, params string[] queues)
{
	await Task.Delay(100);
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	for (int i = 0; i < queues.Length; i++)
	{
		var queuePath = queues[i];
		var queueDescription = await namespaceManager.GetQueueAsync(queuePath);
		queueDescription.MessageCountDetails.ActiveMessageCount.Dump($"Messages in queue `{queuePath}`");
	}
}

static async Task CreateEntities(string connectionString, params string[] queues)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	for (int i = 0; i < queues.Length; i++)
	{
		var queuePath = queues[i];
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