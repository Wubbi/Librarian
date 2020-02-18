using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Librarian.Core;
using NUnit.Framework;

namespace Librarian.Tests
{
    static class Mocks
    {
        #region WebResource

        private static MockHttpClientHandler _mockHttpClientHandler;

        internal static void MockWebResourceHttpClient()
        {
            FieldInfo field = typeof(WebResource).GetField("HttpClient", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field);

            _mockHttpClientHandler = new MockHttpClientHandler();
            field.SetValue(null, new HttpClient(_mockHttpClientHandler));
        }

        internal static void SetWebResourceContent(byte[] data)
        {
            _mockHttpClientHandler.Content = data;
        }

        #endregion //WebResource
    }

    class MockHttpClientHandler : HttpClientHandler
    {
        public byte[] Content { get; set; }

        public MockHttpClientHandler()
        {
            Content = new byte[0];
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Content)
            };

            return Task.FromResult(message);
        }
    }
}
