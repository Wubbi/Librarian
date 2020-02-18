using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Librarian.Core
{
    /// <summary>
    /// A class for downloading data from the world wide web
    /// </summary>
    public class WebResource : IResource
    {
        private static readonly HttpClient HttpClient;

        static WebResource()
        {
            HttpClient = new HttpClient();
        }

        /// <summary>
        /// The source address for this resource
        /// </summary>
        public string Url { get; }

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

        /// <summary>
        /// Whether or not the download was completed. A canceled download will still result in a valid <see cref="WebResource"/>, but won't contain the full data set (it will have the same size though, padded with 0s)
        /// </summary>
        public bool Completed { get; }

        private WebResource(string url, string sha1, long size, MemoryStream data, bool completed)
        {
            Url = url;
            Sha1 = sha1;
            Size = size;
            Data = data;
            Completed = completed;
        }

        /// <summary>
        /// Asynchronously downloads the data from a given URL using a HTTP GET request
        /// </summary>
        /// <param name="url">The target to download</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null</exception>
        /// <exception cref="HttpRequestException">The request failed</exception>
        public static Task<WebResource> LoadAsync([NotNull] string url)
            => LoadAsync(url, CancellationToken.None, delegate { });

        /// <summary>
        /// Asynchronously downloads the data from a given URL using a HTTP GET request
        /// </summary>
        /// <param name="url">The target to download</param>
        /// <param name="updateCallback">A callback method for giving feedback on the download progress</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null</exception>
        /// <exception cref="HttpRequestException">The request failed</exception>
        public static Task<WebResource> LoadAsync([NotNull] string url, [NotNull] DownloadProgressUpdate updateCallback)
            => LoadAsync(url, CancellationToken.None, updateCallback);

        /// <summary>
        /// Asynchronously downloads the data from a given URL using a HTTP GET request
        /// </summary>
        /// <param name="url">The target to download</param>
        /// <param name="token">A token to cancel the download</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null</exception>
        /// <exception cref="HttpRequestException">The request failed</exception>
        public static Task<WebResource> LoadAsync([NotNull] string url, CancellationToken token)
            => LoadAsync(url, token, delegate { });

        /// <summary>
        /// Asynchronously downloads the data from a given URL using a HTTP GET request
        /// </summary>
        /// <param name="url">The target to download</param>
        /// <param name="token">A token to cancel the download</param>
        /// <param name="updateCallback">A callback method for giving feedback on the download progress</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is null</exception>
        /// <exception cref="HttpRequestException">The request failed</exception>
        public static async Task<WebResource> LoadAsync([NotNull] string url, CancellationToken token, [NotNull] DownloadProgressUpdate updateCallback)
        {
            if (url is null)
                throw new ArgumentNullException(nameof(url));

            Uri uri = new Uri(url);

            using HttpResponseMessage response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);

            response.EnsureSuccessStatusCode();

            byte[] data = await DownloadStreamToByteArrayAsync(response, token, updateCallback ?? delegate { });

            return new WebResource(uri.AbsoluteUri, DataHelper.GenerateSha1(data), data.LongLength, new MemoryStream(data, false), !token.IsCancellationRequested);
        }

        private static async Task<byte[]> DownloadStreamToByteArrayAsync(HttpResponseMessage response, CancellationToken token, DownloadProgressUpdate updateCallback)
        {
            long totalSize = response.Content.Headers.ContentLength.GetValueOrDefault();
            DownloadProgress.Factory factory = new DownloadProgress.Factory(totalSize);

            using Stream responseStream = await response.Content.ReadAsStreamAsync();

            byte[] data = new byte[totalSize];
            using MemoryStream dataStream = new MemoryStream(data);

            byte[] buffer = new byte[8192];
            int totalRead = 0;
            int readBytes;
            while (!token.IsCancellationRequested && (readBytes = await responseStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                updateCallback?.Invoke(factory.Update(totalRead));
                await dataStream.WriteAsync(buffer, 0, readBytes, token);
                totalRead += readBytes;
            }

            updateCallback?.Invoke(factory.Update(totalRead));

            return data;
        }

        public void Dispose()
        {
            Data.Dispose();
        }
    }
}