# Azure Service Bus - High Availability Patterns

Azure Service High Availability Pattern

## Service Bus - Geo-Disaster Recovery

Currently Azure Service Bus in geo-disaster recovery copies the metadata only, but not the data. The secondary region is readonly and will become writable once a failover is initiated.

- https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr

## Active-Active Approach

- Create two namespaces in different regions
- Create a queue with the same name in the different regions
- Send a message to both regions
- Process message from both regions
- Have logic to determine if message has already been processed

### Emmitter Logic

Try to write message to both regions

```c#
private static async Task SendMessageHAAsync(Message message)
{
    try
    {
        await primaryQueueClient.SendAsync(message);
    }
    catch (Exception)
    {
        //throw;
    }
    try
    {
        await secondaryQueueClient.SendAsync(message);
    }
    catch (Exception)
    {
        //throw;
    }
}
```

### Receiver Logic

Process from one region, and discard from second region if message has already been processed.

```c#
static void RegisterOnMessageHandlerAndReceiveMessages()
{
    // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
    var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
    {
        // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
        // Set it according to how many messages the application wants to process in parallel.
        MaxConcurrentCalls = 2,

        // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
        // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
        AutoComplete = false
    };
    // Register the function that will process messages
    primaryQueueClient?.RegisterMessageHandler(PrimaryProcessMessagesAsync, messageHandlerOptions);
    secondaryQueueClient?.RegisterMessageHandler(SecondaryProcessMessagesAsync, messageHandlerOptions);
}

static async Task PrimaryProcessMessagesAsync(Message message, CancellationToken token)
{
    // Process the message
    Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");
    var jobid = (Guid)message.UserProperties[Common.Contants.IdProperty];
    if (!IsProcessed(jobid))
    {
        Console.WriteLine($"{jobid}: Processing from Primary");
    }
    // Complete the message so that it is not received again.
    // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
    await primaryQueueClient.CompleteAsync(message.SystemProperties.LockToken);
    // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
    // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
    // to avoid unnecessary exceptions.
}
```

### Messages Already Processed Logic

This sample app uses a simple approach to keep state in memory.

```c#
static object obj = new object();
static List<Guid> State = new List<Guid>();

static bool IsProcessed(Guid guid)
{
    lock (obj)
    {
        if (State.Contains(guid))
            return true;
        State.Add(guid);
        return false;
    }
}
```

## Active-Passive Approach