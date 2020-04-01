namespace MQ.Receiver
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;

    class Program
    {
        // Connection String for the namespace can be obtained from the Azure portal under the 
        // 'Shared Access policies' section.
        const string PrimaryServiceBusConnectionString = "";
        const string SecondaryServiceBusConnectionString = "";
        const string QueueName = "job-queue";
        static IQueueClient primaryQueueClient;
        private static bool primaryAvailable;
        static IQueueClient secondaryQueueClient;
        private static bool secondaryAvailable;
        static object obj = new object();
        static List<Guid> State = new List<Guid>();

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            InitializeQueuesAsync();

            if (!primaryAvailable && !secondaryAvailable)
            {
                Console.WriteLine("Primary and secondary regions are not availble to process messages");
                Environment.Exit(-1);
            }


            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
            Console.WriteLine("======================================================");

            // Register QueueClient's MessageHandler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await CloseQueuesAsync();
        }

        private static void InitializeQueuesAsync()
        {
            try
            {
                primaryQueueClient = new QueueClient(Common.Contants.PrimaryConnectionString, QueueName);
                primaryAvailable = true;
            }
            catch (Exception)
            {
            }
            try
            {
                secondaryQueueClient = new QueueClient(Common.Contants.SecondaryConnectionString, QueueName);
                secondaryAvailable = true;
            }
            catch (Exception)
            {
            }
        }

        private static async Task CloseQueuesAsync()
        {
            try
            {
                await primaryQueueClient.CloseAsync();
            }
            catch (Exception)
            {
            }
            try
            {
                await secondaryQueueClient.CloseAsync();
            }
            catch (Exception)
            {
            }
        }

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
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(Common.Tools.Compression.Decompress(message.Body))}");

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

        static async Task SecondaryProcessMessagesAsync(Message message, CancellationToken token)
        {


            // Process the message
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(Common.Tools.Compression.Decompress(message.Body))}");

            var jobid = (Guid)message.UserProperties[Common.Contants.IdProperty];
            if (!IsProcessed(jobid))
            {
                Console.WriteLine($"{jobid}: Processing from Secondary");
            }

            // Complete the message so that it is not received again.
            // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
            await secondaryQueueClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls 
            // to avoid unnecessary exceptions.
        }

        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }
    }
}
