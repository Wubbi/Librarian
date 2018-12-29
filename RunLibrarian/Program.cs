namespace RunLibrarian
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Librarian.Librarian librarian = new Librarian.Librarian("../../../../settings.json");

            librarian.Run();
        }
    }
}
