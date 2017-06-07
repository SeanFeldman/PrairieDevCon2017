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

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);

	var messageReceivier = await messagingFactory.CreateMessageReceiverAsync("myqueue", ReceiveMode.PeekLock);

	// message pump options
	var onMessageOptions = new OnMessageOptions
	{
		AutoComplete = false,
		AutoRenewTimeout = TimeSpan.FromMinutes(2),
		MaxConcurrentCalls = 5
	};

	// exception handler
	onMessageOptions.ExceptionReceived += (sender, exceptionReceivedEventArgs) =>
	{
		exceptionReceivedEventArgs.Exception.Dump($"Pump encountered an exception, action={exceptionReceivedEventArgs.Action}", 1, true); 
	};

	// message pump
	messageReceivier.OnMessageAsync(async message =>
	{
		($"Received message with body: {message.GetBody<string>()}").Dump();
		await message.CompleteAsync();
		
	}, onMessageOptions);


	// send messages
	var messageSender = await messagingFactory.CreateMessageSenderAsync("myqueue");
	var batch = new List<BrokeredMessage>();
	var runningCount = 1;
	while (runningCount < 50)
	{
		for (int i = 0; i < 10; i++)
		{
			batch.Add(new BrokeredMessage("Message #" + runningCount++));
		}
		await messageSender.SendBatchAsync(batch);
		$"Batch #{runningCount / 10} sent".Dump();
		batch.Clear();
	}

	Util.ReadLine();
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

	$"Queue `{queuePath}`, topic `{topicPath}, and subscription `{subscriptionName}` created\n".Dump();
}

static async Task EnsureQueueDoesNotExist(NamespaceManager ns, string queuePath)
{
	if (await ns.QueueExistsAsync(queuePath))
	{
		await ns.DeleteTopicAsync(queuePath);
	}
}

static async Task EnsureTopicDoesNotExist(NamespaceManager ns, string topicPath)
{
	if (await ns.TopicExistsAsync(topicPath))
	{
		await ns.DeleteTopicAsync(topicPath);
	}
}