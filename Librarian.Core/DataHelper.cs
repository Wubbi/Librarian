using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Librarian.Core
{
    public static class DataHelper
    {
        internal static string GenerateSha1(byte[] data)
        {
            using SHA1Managed sha1Managed = new SHA1Managed();
            byte[] hash = sha1Managed.ComputeHash(data);
            return string.Join("", hash.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        public static string AsString(this IResource resource, Encoding encoding)
        {
            StreamReader streamReader = new StreamReader(resource.Data, encoding);
            return streamReader.ReadToEnd();
        }
    }
}
