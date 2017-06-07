<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	Process.Start(@"C:\Users\Sean\OneDrive\Documents\Presintations\Prairie DevCon 2017\ASB\snippets\07.sending.and.receiving_AFTER.linq");
	MainAsync().GetAwaiter().GetResult();
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	await CreateEntities(connectionString, "mytopic", "mysub");
	
	var body = "An event";
	var msg = new BrokeredMessage(body);

	var topicClient = TopicClient.CreateFromConnectionString(connectionString, "mytopic");
	await topicClient.SendAsync(msg);

	($"Message with body `{body}` sent").Dump();

	var subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, "mytopic", "mysub", ReceiveMode.PeekLock);
	var receivedEvent = await subscriptionClient.ReceiveAsync();
	
	await receivedEvent.CompleteAsync();
	($"Received message with body `{receivedEvent.GetBody<string>()}`").Dump();
}

static async Task CreateEntities(string connectionString, string topicPath, string subscriptionName)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
	await EnsureTopicDoesNotExist(namespaceManager, topicPath);
	await namespaceManager.CreateTopicAsync(new TopicDescription(topicPath));
	var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
	await namespaceManager.CreateSubscriptionAsync(subscriptionDescription);
	$"Topic `{topicPath}` and subscription `{subscriptionName}` created".Dump();
}

static async Task EnsureTopicDoesNotExist(NamespaceManager namespaceManager, string topicPath)
{
	if (await namespaceManager.TopicExistsAsync(topicPath))
	{
		await namespaceManager.DeleteTopicAsync(topicPath);
	}
}