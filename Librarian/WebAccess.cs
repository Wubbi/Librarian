using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Librarian
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
        /// <param name="file">The file as which to save the downloaded data, or null to generate a name from the <paramref name="url"/>. Existing files are overwritten</param>
        /// <param name="expectedSize">The expected size of the file in bytes</param>
        /// <returns>The name of the downloaded file</returns>
        /// <exception cref="ArgumentException"><paramref name="file"/> is null but no filename could be generated from the <paramref name="url"/></exception>
        /// <exception cref="InvalidDataException">The file size does not match <paramref name="expectedSize"/></exception>
        public static string DownloadAndStoreFile(string url, string file = null, long expectedSize = 0)
        {
            if (file == null)
            {
                file = url.Substring(url.LastIndexOf('/') + 1);

                if (file.Length <= 0 || file.Intersect(Path.GetInvalidFileNameChars()).Any())
                    throw new ArgumentException("Could not get valid filename from URL");
            }

            byte[] downloadFile = DownloadFile(url, expectedSize);

            File.WriteAllBytes(file, downloadFile);

            return file;
        }

        /// <summary>
        /// Downloads a file from an url and converts it into a string
        /// </summary>
        /// <param name="url">The location of the file as url</param>
        /// <param name="expectedSize">The expected size of the file in bytes</param>
        /// <param name="encoding">The encoding to use for the conversion or null for UTF8</param>
        /// <returns>The content of the file as string</returns>
        /// <exception cref="InvalidDataException">The file size does not match <paramref name="expectedSize"/></exception>
        public static string DownloadFileAsString(string url, long expectedSize = 0, Encoding encoding = null)
        {
            byte[] downloadedFile = DownloadFile(url, expectedSize);

            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(downloadedFile);
        }

        /// <summary>
        /// Downloads a file and stores it in a byte array
        /// </summary>
        /// <param name="url">The location of the file as url</param>
        /// <param name="expectedSize">The expected size of the file in bytes</param>
        /// <returns>The file as byte array</returns>
        /// <exception cref="InvalidDataException">The file size does not match <paramref name="expectedSize"/></exception>
        public static byte[] DownloadFile(string url, long expectedSize = 0)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] downloadedData = webClient.DownloadData(url);

                if (expectedSize > 0L && downloadedData.LongLength != expectedSize)
                    throw new InvalidDataException("The downloaded data does not match the expected size");

                return downloadedData;
            }
        }
    }
}
