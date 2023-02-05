using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerSpace
{
    internal class Shell
    {
        internal bool IsPipeline;

        private Runspace _Runspace;
        private Pipeline _Pipeline;
        private PowerShell _PowerShell;

        internal Shell(bool isPipeline, string prologue = null)
        {
            IsPipeline = isPipeline;

            _Pipeline = null;
            _PowerShell = null;
            _Runspace = RunspaceFactory.CreateRunspace();

            _Runspace.Open();

            if (IsPipeline)
                _Pipeline = _Runspace.CreatePipeline();
            else
            {
                _PowerShell = PowerShell.Create();
                _PowerShell.Runspace = _Runspace;
            }

            if (prologue != null)
                Invoke(prologue);
        }

        ~Shell()
        {
            Close();
        }

        internal string BuiltinCD(string command)
        {
            string[] args = command.Split(' ');

            if (args.Length != 2)
                throw new Exception("Usage: cd [directory]");

            Directory.SetCurrentDirectory(args[1]);

            return Invoke(command);
        }

        internal string BuiltinReset()
        {
            Reset();
            return "[PowerSpace] Shell successfully reset\n";
        }

        internal static string Prompt(bool colorized=false)
        {
            string path = Directory.GetCurrentDirectory();
            // string user = Environment.UserName;
            // string host = Environment.MachineName;

            if (colorized)
                return "\x1b[90m[\x1b[91mPowerSpace\x1b[90m] " + path + "\x1b[0m> ";

            return "[PowerSpace] " + path + "> ";

        }

        internal string Invoke(string command)
        {
            string output = "";

            if (IsPipeline)
            {
                _Pipeline.Commands.AddScript(command);

                try
                {
                    foreach (PSObject pso in _Pipeline.Invoke())
                        output += pso.ToString() + "\n";

                    foreach (object e in _Pipeline.Error.ReadToEnd())
                        output += e.ToString() + "\n";
                }
                catch (Exception e)
                {
                    output += e.ToString() + "\n";
                }

                _Pipeline.Commands.Clear();

                _Pipeline.Dispose();
                _Pipeline = _Runspace.CreatePipeline();
            }
            else
            {
                _PowerShell.AddScript(command);

                foreach (PSObject pso in _PowerShell.Invoke())
                    output += pso.ToString() + "\n";

                foreach (ErrorRecord e in _PowerShell.Streams.Error)
                    output += e.ToString() + "\n";

                _PowerShell.Commands.Clear();
                _PowerShell.Streams.ClearStreams();
            }

            return output;
        }

        internal void Close()
        {
            if (_Pipeline != null)
            {
                _Pipeline.Dispose();
                _Pipeline = null;
            }

            if (_PowerShell != null)
            {
                _PowerShell.Dispose();
                _PowerShell = null;
            }

            if (_Runspace != null)
            {
                _Runspace.Close();
                _Runspace = null;
            }
        }

        internal void Reset()
        {
            if (_Pipeline != null)
            {
                _Pipeline.Commands.Clear();
                _Pipeline.Dispose();

                _Pipeline = _Runspace.CreatePipeline();

                return;
            }

            if (_PowerShell != null)
            {
                _PowerShell.Dispose();

                _PowerShell = PowerShell.Create();
                _PowerShell.Runspace = _Runspace;
            }
        }
    }
}
