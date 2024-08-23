using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Pulsar.Common;

namespace Pulsar.Producer;

class Program
{
    static async Task Main(string[] args)
    {
        int minIoc;
        ThreadPool.GetMinThreads(out _, out minIoc);
        ThreadPool.SetMinThreads(20, minIoc);

        const string serviceUrl = "pulsar://localhost:6650";
        const string topicName = "persistent://public/default/persons";

        var schema = Schema.AVRO<Person>();

        var tasksCount = 1;
        var messagesPerTask = 1;
        
        if(args.Length >= 1)
        {
            messagesPerTask = Convert.ToInt32(args[0]);
        }
        
        if(args.Length >= 2)
        {
            tasksCount = Convert.ToInt32(args[1]);
        }
        
        var client = await new PulsarClientBuilder()
            .ServiceUrl(serviceUrl)
            .BuildAsync();
        var producer = await client.NewProducer(schema)
            .ProducerName("person-generator")
            .Topic(topicName)
            .BlockIfQueueFull(true)
            .MessageRoutingMode(MessageRoutingMode.RoundRobinPartition)
            .CreateAsync();
        
        var tasks = new Task[ tasksCount];

        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Publishing with {tasksCount} threads");

        for (int taskId = 0; taskId <  tasksCount; taskId++)
        {
            var senderTaskId = taskId;
            tasks[taskId] = Task.Run(async() =>
            {
                while (true)
                {
                    for (int personId = 0; personId < messagesPerTask; personId++)
                    {
                        var person = GeneratePerson(senderTaskId, personId);

                        var key = GenerateKey(person);
                        var message = producer
                            .NewMessage(person, key)
                            //.WithOrderingKey(key)
                            ;
                        Console.WriteLine($"Publishing message with the key {message.Key}");
                        
                        await producer    
                            .SendAsync(message);
                    }
                    
                    Thread.Sleep(1000);
                }
            });
        }

        Task.WaitAll(tasks);

        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Publishing done, {tasksCount * messagesPerTask} messages published");

    }

    private static string GenerateKey(Person person)
    {
        return person.Country;
    }
    
    private static Person GeneratePerson(int senderId, int personId)
    {
        return new Person()
        {
            Id = personId,            
            Name = $"{senderId}-{personId}",
            Country = (personId % 3) switch { 0 => "USA", 1 => "UK", _ => "China" }
        };
    }
}