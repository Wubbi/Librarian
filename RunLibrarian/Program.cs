using System;
using Librarian;

namespace RunLibrarian
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger.NewLogEntry += Console.WriteLine;

            Librarian.Librarian librarian = new Librarian.Librarian(args.Length > 0 ? args[0] : "settings.json");

            librarian.Run();
        }
    }
}
