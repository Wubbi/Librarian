using System;
using System.IO;

namespace RunLibrarian
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (args.Length <= 1 || args[1].ToLowerInvariant() != "-output")
                    Logger.NewLogEntry += Console.WriteLine;

                string settingsFile = args.Length > 0 ? args[0] : "settings.json";

                if (!File.Exists(settingsFile))
                    File.WriteAllText(settingsFile,
                        @"
{
	refreshRate:29,
	libraryPath:"""",
	addMissingVersions:false,
	tasks:
	[
	]
}
                    "
                    );

                com.github.Wubbi.Librarian.Librarian librarian = new com.github.Wubbi.Librarian.Librarian(settingsFile);

                librarian.Run();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
    }
}
