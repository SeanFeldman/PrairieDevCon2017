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
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

	await CreateEntities(namespaceManager, "myqueue", "mytopic", "mysub1", "mysub2");

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);

	// send events
	var messageSender = await messagingFactory.CreateMessageSenderAsync("mytopic");
	for (int i = 0; i < 10; i++)
	{
		var @event = new BrokeredMessage($"Message # {i + 1}");
		@event.Label = i % 5 == 0 ? "rush" : "";
		@event.Properties["Amount"] = i % 3 == 0 ? (100 + i * 10) : (100 - i * 10);

		await messageSender.SendAsync(@event);
		$"Label:`{@event.Label}`, Amount:{@event.Properties["Amount"]}".Dump($"Message #{i + 1} sent", 1, true);
	}
}


static async Task CreateEntities(NamespaceManager nsm, string queuePath, string topicPath, string subscriptionName1, string subscriptionName2)
{
	await EnsureQueueDoesNotExist(nsm, queuePath);
	await EnsureTopicDoesNotExist(nsm, topicPath);
	await nsm.CreateQueueAsync(new QueueDescription(queuePath));
	await nsm.CreateTopicAsync(new TopicDescription(topicPath));

	var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName1);
	var ruleDescription1 = new RuleDescription("HighPriority", new SqlFilter("sys.Label LIKE '%rush%' OR Amount >= 100"));
	ruleDescription1.Action = new SqlRuleAction("SET Priority='high'");
	await nsm.CreateSubscriptionAsync(subscriptionDescription, ruleDescription1);

	subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName2);
	var ruleDescription2 = new RuleDescription("NormalPriority", new SqlFilter("(Amount < 100) AND (sys.Label NOT LIKE '%rush%')"));
	ruleDescription2.Action = new SqlRuleAction("SET Priority='normal'");
	await nsm.CreateSubscriptionAsync(subscriptionDescription, ruleDescription2);
}

static Task CreateWireTap(NamespaceManager namespaceManager, string topicPath) 
{
	var sd = new SubscriptionDescription(topicPath, "wire-tap") { AutoDeleteOnIdle = TimeSpan.FromMinutes(5) };
	return namespaceManager.CreateSubscriptionAsync(sd);
}

static async Task EnsureQueueDoesNotExist(NamespaceManager ns, string queuePath)
{
	if (await ns.QueueExistsAsync(queuePath))
	{
		$"Queue `{queuePath}` was found".Dump();
		await ns.DeleteTopicAsync(queuePath);
		"and was deleted".Dump();
	}
}

static async Task EnsureTopicDoesNotExist(NamespaceManager ns, string topicPath)
{
	if (await ns.TopicExistsAsync(topicPath))
	{
		$"Topic `{topicPath}` was found".Dump();
		await ns.DeleteTopicAsync(topicPath);
		"and was deleted".Dump();
	}
}