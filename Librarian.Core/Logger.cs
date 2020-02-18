using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Librarian.Core
{
    public class Logger
    {
        public static Logger Instance { get; private set; }

        static Logger()
        {
            Instance = new Logger();
        }

        public enum Level
        {
            Info,
            Warning,
            Error
        }

        public string LogFile { get; }

        private Logger()
        {
            LogFile = "";
        }

        public Logger([NotNull] string logFile)
        {
            LogFile = logFile ?? throw new ArgumentNullException(nameof(logFile));

            Log("Initializing Logger");
        }

        private void WriteLine(string line, Level level)
        {
            using FileStream fileStream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.WriteLine(line);
            streamWriter.Flush();
        }

        public void Log([NotNull] string message, Level level = Level.Info)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            string line = $"[{DateTime.UtcNow:s}] [{level}] {message}";

            Debug.WriteLine(line);

            if (LogFile.Length > 0)
                WriteLine(line, level);
        }

        public static void SetLogger([NotNull] Logger logger)
        {
            Instance = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
