using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace VersionOne.Provisioning.Logging
{
    public class Logger
    {
        private string _logPath;

        public Logger(string logPath)
        {
            this._logPath = logPath;
        }

        public void LogToTextFile(StringBuilder message)
        {
            string textToWrite = message.ToString();
            string path = _logPath;
            using (FileStream fstream = new FileStream(path, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter swriter = new StreamWriter(fstream))
                {

                    swriter.Write(textToWrite);
                    message.Remove(0, message.Length);
                    swriter.Close();
                }
            }
        }
    }
}
