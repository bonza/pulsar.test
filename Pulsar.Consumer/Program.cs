using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Pulsar.Common;

namespace Pulsar.Consumer;

class Program
{
    static async Task Main(string[] args)
    {
        const string serviceUrl = "pulsar://localhost:6650";

        const string topicName = "persistent://public/default/persons";
        const string subscriptionName = "key-shared";

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var consumerName = "consumer";
        
        if(args.Length > 0)
        {
            consumerName = $"consumer-{args[0]}";
        }

        var consumerTask = Task.Run(async () =>
        {
            var client = await new PulsarClientBuilder()
                .ServiceUrl(serviceUrl)
                .BuildAsync();

            var consumer = await client.NewConsumer(Schema.AVRO<Person>())
                .Topic(topicName)
                .SubscriptionName(subscriptionName)
                .SubscriptionType(SubscriptionType.KeyShared)
                .KeySharedPolicy(KeySharedPolicy.KeySharedPolicyAutoSplit())
                //.SubscriptionInitialPosition(subscriptionInitialPosition:SubscriptionInitialPosition.Latest)
                .ConsumerName(consumerName)
                .SubscribeAsync();

            Console.WriteLine($"Starting consumer '{consumer.Name}' with id {consumer.ConsumerId}");
            
            long msgTotal = 0;
            long msgCounter = 0;
            long errCounter = 0;
            var start = DateTime.Now;
            while (!cancellationToken.IsCancellationRequested)
            {
                Message<Person> message = null;
                try
                {
                    message = await consumer.ReceiveAsync(cancellationToken);
                    var person = message.GetValue();

                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Received person: {person.Id}. Key: {message.Key}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch(Exception)
                {
                    errCounter++;
                }
                finally
                {
                    if (message != null)
                    {
                        await consumer.AcknowledgeAsync(message.MessageId);
                    }
                }

                msgCounter++;
                msgTotal++;

                var timeSpan = DateTime.Now - start;
                if (timeSpan.TotalSeconds >= 5)
                {
                    Console.WriteLine($"Receiving speed is {msgCounter/5} ms/sec");
                    start = DateTime.Now;
                    msgCounter = 0;
                }
                else
                {
                    // Console.WriteLine(timeSpan.TotalSeconds);
                }
            }

            Console.WriteLine($"Received {msgTotal} messages, {errCounter} of errors");

            // Dispose the consumer
            await consumer.DisposeAsync();

        }, cancellationToken);

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        cancellationTokenSource.Cancel(); // Request cancellation
        await consumerTask;

        Console.WriteLine("Consuming completed");
    }
}