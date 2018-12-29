using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Librarian
{
    public class Logger
    {
        private static string _logFilePath;

        public static event Action<string> NewLogEntry;

        public static void SetLogFilePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            //Check if we can properly use the logfile by opening it and adding a line
            using (FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
            {
                streamWriter.WriteLine($"[{DateTime.Now.ToUniversalTime():u}] #### Accessing log ####");
            }

            _logFilePath = path;
        }

        private static void Log(string data)
        {
            data = data.Replace(Environment.NewLine, "\t" + Environment.NewLine);

            data = $"[{DateTime.Now.ToUniversalTime():u}] {data}";

            Debug.WriteLine(data);

            NewLogEntry?.Invoke(data);

            if (_logFilePath != null)
                File.AppendAllText(_logFilePath, data + Environment.NewLine, Encoding.UTF8);
        }

        public static void Info(string message)
        {
            Log($"[INFO] {message}");
        }

        public static void Error(string errorMessage)
        {
            Log($"[ERROR] {errorMessage}");
        }

        public static void Exception(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[EXCEPTION] ");
            sb.AppendLine(exception.ToString());

            foreach (DictionaryEntry dataEntry in exception.Data)
                sb.AppendLine($"[{dataEntry.Key}] {dataEntry.Value}");

            Log(sb.ToString());
        }
    }
}
