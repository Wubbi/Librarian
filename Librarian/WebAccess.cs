using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.github.Wubbi.Librarian
{
    /// <summary>
    /// Responsible for handling all traffic in and out the internet
    /// </summary>
    public static class WebAccess
    {
        /// <summary>
        /// Downloads a file from an url and stores it on the local filesystem
        /// </summary>
        /// <param name="url">The location of the file as url</param>
        /// <param name="file">The file as which to save the downloaded data. If it is null or a directory the name is generated from the <paramref name="url"/>. Existing files are overwritten</param>
        /// <param name="expectedSize">The expected size of the file in bytes or 0 if the length should not be checked</param>
        /// <param name="sha1">The expected SHA1 hash of the file or null if the hash should not be compared</param>
        /// <returns>The name of the downloaded file</returns>
        /// <exception cref="ArgumentException"><paramref name="file"/> is null or a directory but no filename could be generated from the <paramref name="url"/></exception>
        /// <exception cref="InvalidDataException">The file size or its hash do not match the expected values</exception>
        public static string DownloadAndStoreFile(string url, string file = null, long expectedSize = 0, string sha1 = null)
        {
            if (file == null || Directory.Exists(file))
            {
                string fileName = url.Substring(url.LastIndexOf('/') + 1);

                if (fileName.Length <= 0 || fileName.Intersect(Path.GetInvalidFileNameChars()).Any())
                    throw new ArgumentException("Could not get valid filename from URL");

                if (Directory.Exists(file))
                    file = Path.Combine(file, fileName);
                else
                    file = fileName;
            }

            byte[] downloadFile = DownloadFile(url, expectedSize, sha1);

            File.WriteAllBytes(file, downloadFile);

            return file;
        }

        /// <summary>
        /// Downloads a file from an url and converts it into a string
        /// </summary>
        /// <param name="url">The location of the file as url</param>
        /// <param name="encoding">The encoding to use for the conversion or null for UTF8</param>
        /// <param name="expectedSize">The expected size of the file in bytes or 0 if the length should not be checked</param>
        /// <param name="sha1">The expected SHA1 hash of the file or null if the hash should not be compared</param>
        /// <returns>The content of the file as string</returns>
        /// <exception cref="InvalidDataException">The file size or its hash do not match the expected values</exception>
        public static string DownloadFileAsString(string url, Encoding encoding = null, long expectedSize = 0, string sha1 = null)
        {
            byte[] downloadedFile = DownloadFile(url, expectedSize, sha1);

            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(downloadedFile);
        }

        /// <summary>
        /// Downloads a file and stores it in a byte array
        /// </summary>
        /// <param name="url">The location of the file as url</param>
        /// <param name="expectedSize">The expected size of the file in bytes or 0 if the length should not be checked</param>
        /// <param name="sha1">The expected SHA1 hash of the file or null if the hash should not be compared</param>
        /// <returns>The file as byte array</returns>
        /// <exception cref="InvalidDataException">The file size or its hash do not match the expected values</exception>
        public static byte[] DownloadFile(string url, long expectedSize = 0, string sha1 = null)
        {
            byte[] downloadedData;
            using (WebClient webClient = new WebClient())
            {
                DateTime timestamp = DateTime.Now;
                int progress = 0;
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    if ((DateTime.Now - timestamp).TotalSeconds < 5)
                        return;

                    if (e.ProgressPercentage < progress + 10 && (DateTime.Now - timestamp).TotalSeconds < 15)
                        return;

                    timestamp = DateTime.Now;
                    progress = e.ProgressPercentage;

                    Logger.Info($"Download progress: {progress:###.}%");
                };

                downloadedData = webClient.DownloadDataTaskAsync(url).Result;
            }

            if (expectedSize > 0L && downloadedData.LongLength != expectedSize)
                throw new InvalidDataException($"The downloaded data ({downloadedData.LongLength} Bytes) does not match the expected size ({expectedSize} Bytes)");

            if (sha1 != null && GenerateSha1HexString(downloadedData) != sha1)
                throw new InvalidDataException("The SHA1 hash of the download does not match the expected one");

            return downloadedData;
        }

        /// <summary>
        /// Takes a bytearray, generates a SHA1 hash of it and returns the result as a hexadecimal string
        /// </summary>
        /// <param name="data">The data to generate the hash from</param>
        /// <returns>A string of hexadecimal values</returns>
        public static string GenerateSha1HexString(byte[] data)
        {
            using (SHA1Managed sha1Managed = new SHA1Managed())
            {
                byte[] sha1 = sha1Managed.ComputeHash(data);
                return string.Join("", sha1.Select(b => b.ToString("x2")));
            }
        }
    }
}
