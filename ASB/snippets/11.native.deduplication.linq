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

	await CreateEntities(connectionString, "dedup-queue");

	var id = "12345";
	var body = "debit $100";
	var msg = new BrokeredMessage(body)
	{
		MessageId = id
	};
	var clone = msg.Clone();

	var queueClient = QueueClient.CreateFromConnectionString(connectionString, "dedup-queue");
	await queueClient.SendAsync(msg);
	await Task.Delay(1000);
	await queueClient.SendAsync(clone);

	($"Message with id `{id}` sent twice").Dump();

	var queueDescription = await NamespaceManager.CreateFromConnectionString(connectionString).GetQueueAsync("dedup-queue");
	queueDescription.MessageCountDetails.ActiveMessageCount.Dump("Messages in `dedup-queue`");
}

static async Task CreateEntities(string connectionString, string queuePath)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

	await EnsureQueueDoesNotExist(namespaceManager, queuePath);
	var queueDescription = new QueueDescription(queuePath)
	{
		RequiresDuplicateDetection = true,
		DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(5)
	};
	await namespaceManager.CreateQueueAsync(queueDescription);
	$"Queue `{queuePath}` created\n".Dump();
}

static async Task EnsureQueueDoesNotExist(NamespaceManager namespaceManager, string queuePath)
{
	if (await namespaceManager.QueueExistsAsync(queuePath))
	{
		await namespaceManager.DeleteTopicAsync(queuePath);
	}
}