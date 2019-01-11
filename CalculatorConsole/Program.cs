using System;
using System.Text;
using RabbitMQ.Client;
using System.Linq;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace CalculatorInterface
{

    public struct Expression
    {
        public long id;
        public string message;
    }

    class Program
    {
       
        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            var factory = new ConnectionFactory() { HostName = "rabbithost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare("expr_to_calc", false, false, false, null);
            channel.QueueDeclare("calculated_result", false, false, false, null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Expression expr = JsonConvert.DeserializeObject<Expression>(message);
                Console.WriteLine(expr.message);
            };
            channel.BasicConsume(queue: "calculated_result", autoAck: true, consumer: consumer);
            Console.WriteLine("Input data to calculate");
            while (true)
            {
                var input = Console.ReadLine();

                if (input == "")
                {
                    Console.WriteLine("Input is empty, will exit now");
                    Environment.Exit(0);
                }
                string trimmed = new string (input.Where(x => !char.IsWhiteSpace(x)).ToArray());
                Expression expr = new Expression() { id = DateTime.Now.Ticks, message = trimmed };
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(Expression));
                var serialized = JsonConvert.SerializeObject(expr);

                var body = Encoding.UTF8.GetBytes(serialized);

                channel.BasicPublish(exchange: "",
                                     routingKey: "expr_to_calc",
                                     basicProperties: null,
                                     body: body);
            }

        }



        static void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Closing app...");
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Environment.Exit(0);
            }

        }
    }
}
