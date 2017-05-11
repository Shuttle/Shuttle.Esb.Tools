using System;
using Castle.Windsor;
using Shuttle.Core.Castle;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Tools.TransferMessages
{
    internal class Program
    {
        private static readonly Type TransportMessageType = typeof(TransportMessage);

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

                var sourceQueueUri = GetQueueUri(arguments, "source");
                var destinationQueueUri = GetQueueUri(arguments, "destination");

                if (sourceQueueUri.Equals(destinationQueueUri))
                {
                    throw new ArgumentException("Source queue uri cannot be the same as the destination queue uri.");
                }

                var maximumCount = arguments.Get("count", arguments.Get("c", 0));

                if (!arguments.Contains("quiet") && !arguments.Contains("q"))
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Red, "About to transfer {0} message{1}...",
                        maximumCount == 0 ? "ALL" : maximumCount.ToString(), maximumCount == 1 ? string.Empty : "s");
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Source queue uri:");
                    ColoredConsole.WriteLine(ConsoleColor.White, "   {0}", sourceQueueUri);
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Destination queue uri:");
                    ColoredConsole.WriteLine(ConsoleColor.White, "   {0}", destinationQueueUri);
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Yellow,
                        "Are you sure that you want to continue? [Y]es or [N]o (default is No)");
                    Console.WriteLine();

                    var answer = Console.ReadKey();

                    Console.WriteLine();
                    Console.WriteLine();

                    if (!answer.KeyChar.ToString().Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ColoredConsole.WriteLine(ConsoleColor.Cyan, "No messages transferred.");
                        return;
                    }
                }

                var clear = arguments.Contains("clear");
                var copy = arguments.Contains("copy");

                if (!copy && maximumCount == 0)
                {
                    maximumCount = 30;
                }

                ColoredConsole.WriteLine(ConsoleColor.DarkCyan, "[starting]");

                var container = new WindsorComponentContainer(new WindsorContainer());

                ServiceBus.Register(container);

                var transferCount = 0;

                var queueManager = container.Resolve<IQueueManager>();
                var serializer = container.Resolve<ISerializer>();

                using (ServiceBus.Create(container).Start())
                {
                    var sourceQueue = queueManager.CreateQueue(sourceQueueUri);
                    var destinationQueue = queueManager.CreateQueue(destinationQueueUri);

                    var receivedMessage = sourceQueue.GetMessage();

                    while (receivedMessage != null)
                    {
                        var stream = receivedMessage.Stream;

                        var transportMessage = (TransportMessage) serializer.Deserialize(TransportMessageType, stream);

                        if (clear)
                        {
                            transportMessage.FailureMessages.Clear();

                            stream.Dispose();

                            stream = serializer.Serialize(transportMessage);
                        }

                        destinationQueue.Enqueue(transportMessage, stream);

                        if (!copy)
                        {
                            sourceQueue.Acknowledge(receivedMessage.AcknowledgementToken);
                        }

                        transferCount++;

                        if (transferCount != maximumCount)
                        {
                            receivedMessage = sourceQueue.GetMessage();

                            if (transferCount % 100 == 0)
                            {
                                ColoredConsole.WriteLine(ConsoleColor.DarkCyan, "Transferred {0} messages so far...",
                                    transferCount);
                            }
                        }
                        else
                        {
                            receivedMessage = null;
                        }
                    }

                    sourceQueue.AttemptDispose();
                    destinationQueue.AttemptDispose();

                    ColoredConsole.WriteLine(ConsoleColor.Cyan, "Transferred {0} messages in total.", transferCount);
                }

                queueManager.AttemptDispose();
            }
            catch (Exception ex)
            {
                ColoredConsole.WriteLine(ConsoleColor.Red, ex.AllMessages());
            }
        }

        private static string GetQueueUri(Arguments arguments, string name)
        {
            var uri = arguments.Get(name, arguments.Get(name.Substring(0, 1), string.Empty));

            if (string.IsNullOrEmpty(uri))
            {
                ColoredConsole.WriteLine(ConsoleColor.DarkGreen, "Enter the {0} queue uri:", name);

                uri = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentException(string.Format("Cannot continue with the {0} queue uri.", name));
            }

            return uri;
        }

        private static void ShowHelp()
        {
            Console.WriteLine(@"
Shuttle.Esb.Tools.TransferMessages.exe

Transfers messages from the source queue to the destination queue:

Shuttle.Esb.Tools.TransferMessages.exe 
	/[source|s]={queueUri} 
        - The queue uri where you are transferring the messages from.

	/[destination|d]={queueUri}
        - The queue uri where you are transferring the messages to.

	/clear
        - Clears the `FailureMessage` collection.

    /copy
        - Copies all the messages, leaving the original.

    /[count\c]={count}
        - Transfers the given number of messages or all when 0.  Default is 0 when /copy is excluded (transferring); else 30 for /copy.

    /[quiet|q]
        - Quiet mode.  You will not receive any prompts.
");
        }
    }
}