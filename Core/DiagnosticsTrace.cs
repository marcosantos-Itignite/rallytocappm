using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItigniteRallyIntegration.Core
{
    public class DiagnosticsTrace
    {
        // Define a simple method to write details about the current executing 
        // environment to the trace listener collection.
        public static void WriteEnvironmentInfoToTrace()
        {

            string methodName = "WriteEnvironmentInfoToTrace";

            Trace.Indent();
            Trace.WriteLine(DateTime.Now.ToString() + " - Start of " + methodName);
            Trace.Indent();

            // Write details on the executing environment to the trace output.
            Trace.WriteLine("Operating system: " + System.Environment.OSVersion.ToString());
            Trace.WriteLine("Computer name: " + System.Environment.MachineName);
            Trace.WriteLine("User name: " + System.Environment.UserName);
            Trace.WriteLine("CLR runtime version: " + System.Environment.Version.ToString());
            Trace.WriteLine("Command line: " + System.Environment.CommandLine);

            // Enumerate the trace listener collection and 
            // display details about each configured trace listener.
            Trace.WriteLine("Number of configured trace listeners = " + Trace.Listeners.Count.ToString());

            foreach (TraceListener tl in Trace.Listeners)
            {
                Trace.WriteLine("Trace listener name = " + tl.Name);
                Trace.WriteLine("               type = " + tl.GetType().ToString());
            }

            Trace.Unindent();
            Trace.WriteLine(DateTime.Now.ToString() + " - End of " + methodName);
            Trace.Unindent();

        }

        public static void TraceStart()
        {
            // Write a trace message to all configured trace listeners.
            Trace.WriteLine(DateTime.Now.ToString() + " - Start of Main");

            // Define a trace listener to direct trace output from this method
            // to the console.
            ConsoleTraceListener consoleTracer;

            //// Check the command line arguments to determine which
            //// console stream should be used for trace output.
            //if ((CmdArgs.Length > 0) && (CmdArgs[0].ToString().ToLower().Equals("/stderr")))
            //// Initialize the console trace listener to write
            //// trace output to the standard error stream.
            //{
            //    consoleTracer = new ConsoleTraceListener(true);
            //}
            //else
            //{
                // Initialize the console trace listener to write
                // trace output to the standard output stream.
                consoleTracer = new ConsoleTraceListener();
           // }
            // Set the name of the trace listener, which helps identify this 
            // particular instance within the trace listener collection.
            consoleTracer.Name = "Integration";

            // Write the initial trace message to the console trace listener.
            consoleTracer.WriteLine(DateTime.Now.ToString() + " [" + consoleTracer.Name + "] - Starting output to trace listener.");

            // Add the new console trace listener to 
            // the collection of trace listeners.
            Trace.Listeners.Add(consoleTracer);

            // Call a local method, which writes information about the current 
            // execution environment to the configured trace listeners.
            WriteEnvironmentInfoToTrace();

            // Write the final trace message to the console trace listener.
            consoleTracer.WriteLine(DateTime.Now.ToString() + " [" + consoleTracer.Name + "] - Ending output to trace listener.");

            // Flush any pending trace messages, remove the 
            // console trace listener from the collection,
            // and close the console trace listener.
            Trace.Flush();
            Trace.Listeners.Remove(consoleTracer);
            consoleTracer.Close();

            // Write a final trace message to all trace listeners.
            Trace.WriteLine(DateTime.Now.ToString() + " - End of Main");

            // Close all other configured trace listeners.
            Trace.Close();

        }


    }
}
