<Query Kind="Program">
  <NuGetReference>WindowsAzure.ServiceBus</NuGetReference>
  <Namespace>Microsoft.ServiceBus</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.ServiceBus.Messaging</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
}

static async Task MainAsync()
{
	var connectionString = Environment.GetEnvironmentVariable("AzureServiceBus.ConnectionString");

	await CreateEntities(connectionString, "myqueue", "mytopic", "mysub");
	
	#region Send/receive with queue	
	
	var body = "Message sent to a queue";
	var msg = new BrokeredMessage(body);
	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
	
	// The gold is here :D
	var messageSender = await messagingFactory.CreateMessageSenderAsync("myqueue");
	await messageSender.SendAsync(msg);
	$"Sent message with body `{body}` to queue".Dump();
	
	var messageReceivier = await messagingFactory.CreateMessageReceiverAsync("myqueue", ReceiveMode.PeekLock);
	
	var receivedMsg = await messageReceivier.ReceiveAsync();
	await receivedMsg.CompleteAsync();
	($"Received message queue `myqueue` with body `{receivedMsg.GetBody<string>()}`\n").Dump();
	
	#endregion

	#region Send/receive with topic/subscription
	
	body = "Message sent to a topic";
	var @event = new BrokeredMessage(body);

	messageSender = await messagingFactory.CreateMessageSenderAsync("mytopic");
	await messageSender.SendAsync(@event);
	$"Sent message with body `{body}` to topic".Dump();

	var subscriptionPath = SubscriptionClient.FormatSubscriptionPath("mytopic", "mysub"); // mytopic/Subscriptions/mysub
	// And here :D
	messageReceivier = await messagingFactory.CreateMessageReceiverAsync(subscriptionPath, ReceiveMode.PeekLock);
	var receivedEvent = await messageReceivier.ReceiveAsync();
	
	await receivedEvent.CompleteAsync();
	($"Received message from subscription `{subscriptionPath}` with body `{receivedEvent.GetBody<string>()}`").Dump();

	#endregion
}

static async Task CreateEntities(string connectionString, string queuePath, string topicPath, string subscriptionName)
{
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

	await EnsureQueueDoesNotExist(namespaceManager, queuePath);
	await EnsureTopicDoesNotExist(namespaceManager, topicPath);
	await namespaceManager.CreateQueueAsync(new QueueDescription(queuePath));
	await namespaceManager.CreateTopicAsync(new TopicDescription(topicPath));
	var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
	await namespaceManager.CreateSubscriptionAsync(subscriptionDescription);
	$"Queue `{queuePath}`, topic `{topicPath}`, and subscription `{subscriptionName}` created\n".Dump();
}

static async Task EnsureQueueDoesNotExist(NamespaceManager namespaceManager, string queuePath)
{
	if (await namespaceManager.QueueExistsAsync(queuePath))
	{
		await namespaceManager.DeleteTopicAsync(queuePath);
	}
}

static async Task EnsureTopicDoesNotExist(NamespaceManager namespaceManager, string topicPath)
{
	if (await namespaceManager.TopicExistsAsync(topicPath))
	{
		await namespaceManager.DeleteTopicAsync(topicPath);
	}
}