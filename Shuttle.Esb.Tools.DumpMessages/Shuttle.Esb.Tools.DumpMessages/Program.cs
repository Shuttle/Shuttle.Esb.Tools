using System;
using System.IO;
using System.Text;
using System.Xml;
using Castle.Windsor;
using Shuttle.Core.Castle;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Esb.Tools.DumpMessages
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

                var queueUri = GetQueueUri(arguments, "source");
                var folder = Path.GetFullPath(arguments.Get("folder", arguments.Get("f", ".\\messages")));

                var maximumCount = int.Parse(arguments.Get("count", arguments.Get("c", "30")));

                if (!arguments.Contains("quiet") && !arguments.Contains("q"))
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Red, "About to dump {0} message{1}...", maximumCount, maximumCount == 1 ? string.Empty : "s");
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Queue uri:");
                    ColoredConsole.WriteLine(ConsoleColor.White, "   {0}", queueUri);
                    ColoredConsole.WriteLine(ConsoleColor.Gray, "Folder:");
                    ColoredConsole.WriteLine(ConsoleColor.White, "   {0}", folder);
                    Console.WriteLine();
                    ColoredConsole.WriteLine(ConsoleColor.Yellow,
                        "Are you sure that you want to continue? [Y]es or [N]o (default is No)");
                    Console.WriteLine();

                    var answer = Console.ReadKey();

                    Console.WriteLine();
                    Console.WriteLine();

                    if (!answer.KeyChar.ToString().Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ColoredConsole.WriteLine(ConsoleColor.Cyan, "No messages dumped.");
                        return;
                    }
                }

                ColoredConsole.WriteLine(ConsoleColor.DarkCyan, "[starting]");

                var container = new WindsorComponentContainer(new WindsorContainer());

                ServiceBus.Register(container);

                var transferCount = 0;

                var queueManager = container.Resolve<IQueueManager>();
                var serializer = container.Resolve<ISerializer>();

                using (ServiceBus.Create(container).Start())
                {
                    var queue = queueManager.CreateQueue(queueUri);

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    var receivedMessage = queue.GetMessage();

                    while (receivedMessage != null)
                    {
                        queue.Release(receivedMessage.AcknowledgementToken);

                        var stream = receivedMessage.Stream;

                        var transportMessage = (TransportMessage)serializer.Deserialize(TransportMessageType, stream);

                        var document = new XmlDocument();

                        document.Load(stream);

                        var root = document.GetElementsByTagName("TransportMessage")[0];
                        var message = root.SelectSingleNode("Message");

                        if (message != null)
                        {
                            message.InnerXml = Encoding.UTF8.GetString(transportMessage.Message);
                        }

                        document.Save(Path.Combine(folder, string.Concat(transportMessage.MessageId.ToString("n"), ".xml")));

                        transferCount++;

                        if (transferCount != maximumCount)
                        {
                            receivedMessage = queue.GetMessage();

                            if (transferCount % 100 == 0)
                            {
                                ColoredConsole.WriteLine(ConsoleColor.DarkCyan, "Dumped {0} messages so far...",
                                    transferCount);
                            }
                        }
                        else
                        {
                            receivedMessage = null;
                        }
                    }

                    queue.AttemptDispose();

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
Shuttle.Esb.Tools.DumpMessages.exe

Dumps messages from the given queue to the destination folder:

Shuttle.Esb.Tools.DumpMessages.exe 
	/[source|s]={queueUri} 
        - The queue uri where you are dumping the messages from.

	/[folder|f]={path-to-folder}
        - The folder where the output messages will be created.  Default is '.\messages'.

    /[count\c]={count}
        - Dumps the given number of messages.  Default is 30.

	/[quiet|q]
        - Quiet mode.  You will not receive any prompts.
");
        }
    }
}