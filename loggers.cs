using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Wox.Plugin.ImprovedClipboard
{
    class Loggers
    {
        private static Loggers logger = new Loggers();
        public static Loggers Logger { get
            {
                return logger;
            } }

        private string logPath = "";
        private string logInfo(string s)
        {
            return DateTime.Now.ToString() + ": " + s;
        }
        public void UpdateLoggerPath(string p)
        {
            p = Path.Combine(p, "logger");
            DirectoryInfo di = new DirectoryInfo(p);
            if (!di.Exists)
            {
                di.Create();
            }
            logPath = Path.Combine(p,"logs.txt");
            if (!File.Exists(logPath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(logPath))
                {
                    sw.WriteLine(logInfo("create log file"));
                }
            }
        }
        public void LogInfo(string s)
        {
            if( this.logPath == "")
            {
                return;
            }
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(logPath))
            {
                sw.WriteLine(logInfo(s));
            }

        }
    }
    
}
