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
	var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

	var messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
	await CreateEntities(namespaceManager, messagingFactory, "mytopic", "mysub");

	// send events
	var messageSender = await messagingFactory.CreateMessageSenderAsync("mytopic");
	var @event = new BrokeredMessage($"Some message");
	@event.Label = "rush";
	@event.Properties["Amount"] = 100;
	await messageSender.SendAsync(@event);
	
	"Sent 1 message with Label='rush' and Amount='100'\n".Dump();
	
	var sub = await namespaceManager.GetSubscriptionAsync("mytopic", "mysub");
	await Task.Delay(500);
	sub.MessageCountDetails.ActiveMessageCount.Dump("Number of messages under `mysub` subscription");
}


static async Task CreateEntities(NamespaceManager namespaceManager, MessagingFactory mf, string topicPath, string subscriptionName)
{
	if (await namespaceManager.TopicExistsAsync(topicPath))
	{
		await namespaceManager.DeleteTopicAsync(topicPath);
		$"Topic `{topicPath}` and subscription `{subscriptionName}` created".Dump();
	}
	await namespaceManager.CreateTopicAsync(new TopicDescription(topicPath));

	var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
	
	var ruleDescription1 = new RuleDescription("Rule1", new SqlFilter("sys.Label LIKE '%rush%'"));
	ruleDescription1.Action = new SqlRuleAction("SET [Rule1.Property]='#1'");

	var ruleDescription2 = new RuleDescription("Rule2", new SqlFilter("Amount >= 100"));
	ruleDescription2.Action = new SqlRuleAction("SET [Rule2.Property]='#2'");
	
	await namespaceManager.CreateSubscriptionAsync(subscriptionDescription, ruleDescription1);
	var client = mf.CreateSubscriptionClient(topicPath, subscriptionDescription.Name);
	await client.AddRuleAsync(ruleDescription2);
}