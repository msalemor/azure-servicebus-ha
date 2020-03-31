
namespace MQ.Emitter
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using MQ.Common.Models;
    using Newtonsoft.Json;

    class Program
    {
        // Connection String for the namespace can be obtained from the Azure portal under the 
        // 'Shared Access policies' section.
        const string QueueName = "job-queue";
        static IQueueClient primaryQueueClient;
        private static IQueueClient secondaryQueueClient;
        private static bool primaryAvailable;
        private static bool secondaryAvailable;

        public static async Task Main(string[] args)
        {
            const int numberOfMessages = 10;
            primaryAvailable = false;
            secondaryAvailable = false;
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

            if (!primaryAvailable && !secondaryAvailable)
            {
                Console.WriteLine("Both the primary and secondary namespaces are not avaible.");
                Environment.Exit(-1);
            }

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after sending all the messages.");
            Console.WriteLine("======================================================");

            // Send messages.
            await SendMessagesAsync(numberOfMessages);

            Console.ReadKey();

            await primaryQueueClient.CloseAsync();
            await secondaryQueueClient.CloseAsync();
        }

        static async Task SendMessagesAsync(int numberOfMessagesToSend)
        {
            try
            {
                var taskList = new List<Task>();
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue
                    var jobId = Guid.NewGuid();

                    // Emulate a custom encoding
                    var bytesBefore = Encoding.UTF8.GetBytes(GetJobInformation(jobId));
                    var bytes = Common.Tools.Compression.Compress(bytesBefore);

                    // prepare the message
                    var message = new Message(bytes);
                    message.UserProperties.Add(Common.Contants.IdProperty, jobId);

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {bytes}");

                    // Send the message to the queue
                    taskList.Add(SendMessageHAAsync(message));
                }
                await Task.WhenAll(taskList);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }

        private static string GetJobInformation(Guid jobId)
        {
            var order = new Order { OrderId = jobId, Message = $"Message Id: {jobId}", Status = OrderStatus.Pending };
            var rnd = new Random(Environment.TickCount);
            var orderDetails = new List<OrderDetail>();

            for(var i=0;i<100;i++)
            {
                var sku = (i + 100).ToString();
                var orderDetail = new OrderDetail { Sku = sku, Description = $"Item Description {sku}", Qty = rnd.Next(1, 100), Price = rnd.Next(10, 1000) };
                orderDetails.Add(orderDetail);
            }

            order.OrderDetails = orderDetails.ToArray();
            
            string messageBody = JsonConvert.SerializeObject(order);
            return messageBody;
        }

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
    }
}
