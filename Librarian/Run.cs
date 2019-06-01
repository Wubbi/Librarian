using System;
using System.IO;

namespace com.github.Wubbi.Librarian
{
    public class Run
    {
        public static void Main(string[] args)
        {
            try
            {
                string settingsFile = "settings.json";
                bool doConsoleOutput = true;

                //for (int i = 0; i < args.Length; ++i)
                //{
                //    switch (args[i].ToLowerInvariant())
                //    {
                //        case "-s":
                //            if (++i < args.Length)
                //                settingsFile = args[i];
                //            break;
                //        case "--o":
                //            doConsoleOutput = false;
                //            break;
                //    }
                //}

                if (!File.Exists(settingsFile))
                    File.WriteAllText(settingsFile,
                        @"
{
	refreshRate:29,
	libraryPath:"""",
	addMissingVersions:false,
	CheckJarFiles:false,
	tasks:
	[
	]
}
                    "
                    );

                using (Librarian librarian = new Librarian(settingsFile, doConsoleOutput))
                { 
                    librarian.Run();
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
    }
}
