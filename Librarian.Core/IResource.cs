using System;
using System.IO;

namespace Librarian.Core
{
    public interface IResource : IDisposable
    {
        string Sha1 { get; }

        long Size { get; }

        MemoryStream Data { get; }
    }
}
