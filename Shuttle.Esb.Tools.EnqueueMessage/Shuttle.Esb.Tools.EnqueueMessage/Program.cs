using System;
using System.IO;
using System.Text;
using Castle.Windsor;
using Shuttle.Core.Castle;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Tools.EnqueueMessage
{
    internal class Program
    {
        private static bool ShouldShowHelp(Arguments arguments)
        {
            Guard.AgainstNull(arguments, "arguments");

            return arguments.Get("help", false) || arguments.Get("h", false) || arguments.Get("?", false);
        }

        private static void Main(string[] args)
        {
            try
            {
                var arguments = new Arguments(args);

                if (ShouldShowHelp(arguments))
                {
                    ShowHelp();

                    return;
                }

                var destinationQueueUri = GetArgument(arguments, "destination", "d", "Enter the destination queue uri");
                var messageType = GetArgument(arguments, "messageType", "mt", "Enter the full type name of the message");
                var path = arguments.Get("path", arguments.Get("p", "message.esb"));

                if (!File.Exists(path))
                {
                    ColoredConsole.WriteLine(ConsoleColor.Red, $"File '{path}' could not be found.");
                    return;
                }

                if (!arguments.Contains("quiet") && !arguments.Contains("q"))
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Red, "About to enqueue message...");
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Message type:");
                    ColoredConsole.WriteLine(ConsoleColor.White, $"   {messageType}");
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Destination queue uri:");
                    ColoredConsole.WriteLine(ConsoleColor.White, $"   {destinationQueueUri}");
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Message file path:");
                    ColoredConsole.WriteLine(ConsoleColor.White, $"   {path}");
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Yellow,
                        "Are you sure that you want to continue? [Y]es or [N]o (default is No)");
                    Console.WriteLine();

                    var answer = Console.ReadKey();

                    Console.WriteLine();
                    Console.WriteLine();

                    if (!answer.KeyChar.ToString().Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ColoredConsole.WriteLine(ConsoleColor.Cyan, "No message enqueued.");
                        return;
                    }
                }

                var container = new WindsorComponentContainer(new WindsorContainer());

                ServiceBus.Register(container);

                var queueManager = container.Resolve<IQueueManager>();
                var transportMessageFactory = container.Resolve<ITransportMessageFactory>();
                var serializer = container.Resolve<ISerializer>();

                using (ServiceBus.Create(container).Start())
                {
                    var destinationQueue = queueManager.CreateQueue(destinationQueueUri);

                    var transportMessage = transportMessageFactory.Create(new object(), c => c.WithRecipient(destinationQueue));

                    transportMessage.AssemblyQualifiedName = messageType;
                    transportMessage.MessageType = messageType;
                    transportMessage.Message = Encoding.UTF8.GetBytes(File.ReadAllText(path));

                    using (var stream = serializer.Serialize(transportMessage))
                    {
                        destinationQueue.Enqueue(transportMessage, stream);
                    }

                    destinationQueue.AttemptDispose();
                }

                queueManager.AttemptDispose();

                ColoredConsole.WriteLine(ConsoleColor.Cyan, "Message enqueued.");
            }
            catch (Exception ex)
            {
                ColoredConsole.WriteLine(ConsoleColor.Red, ex.AllMessages());
            }
        }

        private static string GetArgument(Arguments arguments, string name, string @short, string text)
        {
            var value = arguments.Get(name, arguments.Get(@short, string.Empty));

            if (string.IsNullOrEmpty(value))
            {
                ColoredConsole.WriteLine(ConsoleColor.DarkGreen, $"{text}:");

                value = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Argument {name} is required.");
            }

            return value;
        }

        private static void ShowHelp()
        {
            Console.WriteLine(@"
Shuttle.Esb.Tools.EnqueueMessage.exe

Enqueues the contents of a file as a Shuttle.Esb message into a queue:

Shuttle.Esb.Tools.EnqueueMessage.exe 
	/[destination|d]={queueUri}
        - The queue uri where you are transferring the messages to.

    /[path|p]={file-path}
        - The path to the file containing the message in a deserialized format.  The default value is 'message.txt'.

    /[quiet|q]
        - Quiet mode.  You will not receive any prompts.
");
        }
    }
}