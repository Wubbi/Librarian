using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Librarian.Core
{
    public class LocalResource : IResource
    {
        /// <summary>
        /// The source path for this resource
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The SHA1 hash for this resource as a hexadecimal string
        /// </summary>
        public string Sha1 { get; }

        /// <summary>
        /// The total size of this resource in Bytes
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The data this resource represents
        /// </summary>
        public MemoryStream Data { get; }

        private LocalResource(string path, string sha1, long size, MemoryStream data)
        {
            Path = path;
            Sha1 = sha1;
            Size = size;
            Data = data;
        }

        public static LocalResource Load([NotNull] string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("Could not find file", path);

            byte[] file = File.ReadAllBytes(path);

            MemoryStream stream = new MemoryStream(file, false);

            return new LocalResource(path, DataHelper.GenerateSha1(file), stream.Length, stream);
        }

        public static async Task<LocalResource> LoadAsync([NotNull] string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("Could not find file", path);

            byte[] file = await File.ReadAllBytesAsync(path);

            MemoryStream stream = new MemoryStream(file, false);

            return new LocalResource(path, DataHelper.GenerateSha1(file), stream.Length, stream);
        }

        public void Dispose()
        {
            Data.Dispose();
        }
    }
}
