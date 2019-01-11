using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Linq;

namespace CalculatorCore
{
    public struct Expression
    {
        public long id;
        public string message;
    }

    public class Calculator
    {
        static void Main()
        {
            var factory = new ConnectionFactory() { HostName = "rabbithost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("expr_to_calc", false, false, false, null);
                    channel.QueueDeclare("calculated_result", false, false, false, null);
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" [x] Received {0}", message);
                        Expression expr = JsonConvert.DeserializeObject<Expression>(message);
                        var operatorsCount = expr.message.Where(x => operators.Contains(x.ToString())).Count();
                        const int maxOperatorsCount = 50 - 1;
                        if (operatorsCount >= maxOperatorsCount)
                        {
                            expr = new Expression() { id = DateTime.Now.Ticks, message = "Error" };
                        }
                        else
                        {
                            try
                            {
                                var result = Calculate(expr);
                                expr = new Expression() { id = DateTime.Now.Ticks, message = result.ToString() };
                            }
                            catch
                            {
                                expr = new Expression() { id = DateTime.Now.Ticks, message = "Error" };
                            }
                        }

                        channel.BasicPublish(exchange: "",
                                     routingKey: "calculated_result",
                                     basicProperties: null,
                                     body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(expr)));
                    };
                    channel.BasicConsume(queue: "expr_to_calc",
                                         autoAck: true,
                                         consumer: consumer);
                    

                    Console.ReadLine();
                }
            }

        }

        private static List<string> operators = new List<string> { "+" };

        private static IEnumerable<string> Separate(string input)
        {
            int pos = 0;
            while (pos < input.Length)
            {
                string s = input[pos].ToString();
                if (!operators.Contains(input[pos].ToString()))
                {
                    for (int i = pos + 1; i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'); i++)
                    {
                        s += input[i];
                    }
                    
                }
                yield return s;
                pos += s.Length;
            }
        }

        private static string[] ConvertToPostfixNotation(string input)
        {
            List<string> separatedOutput = new List<string>();
            Stack<string> stack = new Stack<string>();
            foreach (string c in Separate(input))
            {
                if (operators.Contains(c))
                {
                    if (stack.Count > 0)
                    {

                        while (stack.Count > 0)
                        {
                            separatedOutput.Add(stack.Pop());
                        }
                        stack.Push(c);

                    }
                    else
                        stack.Push(c);
                }
                else
                    separatedOutput.Add(c);
            }
            if (stack.Count > 0)
                foreach (string c in stack)
                    separatedOutput.Add(c);

            return separatedOutput.ToArray();
        }
        public static decimal Calculate(Expression exp)
        {
            var input = exp.message;
            Stack<string> stack = new Stack<string>();
            Queue<string> queue = new Queue<string>(ConvertToPostfixNotation(input));
            string str = queue.Dequeue();
            while (queue.Count >= 0)
            {
                if (!operators.Contains(str))
                {
                    stack.Push(str);
                    str = queue.Dequeue();
                }
                else
                {
                    decimal result = 0;
                        switch (str)
                        {

                            case "+":
                                
                                    decimal a = Convert.ToDecimal(stack.Pop());
                                    decimal b = Convert.ToDecimal(stack.Pop());
                                    result = a + b;
                                    break;

                        case "-":
                            throw new NotImplementedException();
                        case "*":
                            throw new NotImplementedException();
                        case "/":
                            throw new NotImplementedException();
                    }
                    stack.Push(result.ToString());
                    if (queue.Count > 0)
                        str = queue.Dequeue();
                    else
                        break;
                }

            }
            return Convert.ToDecimal(stack.Pop());
        }
    }
}
