using Polly.Timeout;
using SimpleLibrary;
using System.Net;

namespace SimpleLibrary.UnitTests
{
    public class HttpClientTest
    {
        private readonly MyHttpClient _client;

        public HttpClientTest()
        {
            _client = new MyHttpClient()
            {
                SleepDurationProvider = (retryAttempt) => TimeSpan.FromMilliseconds(1),
            };
        }

        /// <summary>
        /// Verifies that GetAsync() returns a response with status code 200 (OK) when the server responds quickly.
        /// </summary>
        [Fact]
        public async Task GetAsync_FastServer_ShouldReturn200()
        {
            var httpMessageHandler = new FastResponseHttpMessageHandler();
            GivenMyHttpClient(timeoutSeconds: 1, handler: httpMessageHandler);

            var response = await _client.GetAsync("https://fake.url");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Tests that the GetAsync method retries on receiving a 500 Internal Server Error.
        /// </summary>
        [Fact]
        public async void GetAsync_500Error_ShouldRetry()
        {
            var httpMessageHandler = new InternalServerHttpMessageHandler();
            GivenMyHttpClient(timeoutSeconds: 1, handler: httpMessageHandler);

            var response = await _client.GetAsync("https://fake.url");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(4, httpMessageHandler.SendAsyncCount);
        }

        /// <summary>
        /// Tests that the GetAsync method retries when the server response is too slow.
        /// </summary>
        [Fact]
        public async void GetAsync_SlowServer_ShouldRetry()
        {
            var httpMessageHandler = new SlowResponseHttpMessageHandler(3000);

            await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () =>
            {
                GivenMyHttpClient(timeoutSeconds: 1, handler: httpMessageHandler);
                await _client.GetAsync("https://fake.url");
            });

            Assert.Equal(4, httpMessageHandler.SendAsyncCount);
        }

        [Fact]
        public async Task GetAsync_FastRealServer_ShouldReturn200()
        {
            using var serverFixture = await StartFastServerAsync();
            var httpMessageHandler = new CounterResponseHttpMessageHandler();
            GivenMyHttpClient(timeoutSeconds: 10, handler: httpMessageHandler);

            var response = await _client.GetAsync($"http://localhost:{serverFixture.Port}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, httpMessageHandler.SendAsyncCount);
        }

        /// <summary>
        /// Tests that the GetAsync method retries when the real server response is too slow.
        /// </summary>
        [Fact]
        public async void GetAsync_SlowRealServer_ShouldRetry()
        {
            using var serverFixture = await StartSlowServerAsync();
            var httpMessageHandler = new CounterResponseHttpMessageHandler();

            await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () =>
            {
                GivenMyHttpClient(timeoutSeconds: 1, handler: httpMessageHandler);
                await _client.GetAsync($"http://localhost:{serverFixture.Port}");
            });

            Assert.Equal(4, httpMessageHandler.SendAsyncCount);
        }

        private void GivenMyHttpClient(int timeoutSeconds, HttpMessageHandler handler)
        {
            _client.TimeoutSeconds = timeoutSeconds;
            _client.Handler = handler;
        }

        /// <summary>
        /// Starts a fast server fixture with no delay in processing requests.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the started server fixture.</returns>
        private async Task<SimpleHttpServerFixture> StartFastServerAsync()
        {
            var serverFixture = new SimpleHttpServerFixture(delayMilliseconds: 0);
            await Task.Delay(100); // Wait for the server to start
            return serverFixture;
        }

        /// <summary>
        /// Starts a slow server fixture with a specified delay in processing requests.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the started server fixture.</returns>
        private async Task<SimpleHttpServerFixture> StartSlowServerAsync()
        {
            var serverFixture = new SimpleHttpServerFixture(delayMilliseconds: 2000);
            await Task.Delay(100); // Wait for the server to start
            return serverFixture;
        }

        /// <summary>
        /// A custom HttpMessageHandler that counts the number of times the SendAsync method is called.
        /// </summary>
        public class CounterResponseHttpMessageHandler : HttpClientHandler
        {
            public int SendAsyncCount { get; private set; } = 0;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendAsyncCount++;
                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// FastResponseHttpMessageHandler is a custom HttpMessageHandler that always returns 
        /// an HTTP response with status code 200 (OK).
        /// It is useful for testing scenarios where a fast server response is expected.
        /// </summary>
        public class FastResponseHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        /// <summary>
        /// A custom HttpMessageHandler that always returns a 500 Internal Server Error 
        /// response and counts the number of times the SendAsync method is called.
        /// </summary>
        public class InternalServerHttpMessageHandler : HttpMessageHandler
        {
            public int SendAsyncCount { get; private set; } = 0;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendAsyncCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
        }

        /// <summary>
        /// A custom HttpMessageHandler that simulates a slow server response 
        /// by delaying for a specified time before returning an HTTP 200 OK response, 
        /// and counts the number of times the SendAsync method is called.
        /// </summary>
        public class SlowResponseHttpMessageHandler : HttpMessageHandler
        {
            private readonly int _delayMilliseconds;
            public int SendAsyncCount { get; private set; } = 0;

            public SlowResponseHttpMessageHandler(int delayMilliseconds)
            {
                _delayMilliseconds = delayMilliseconds;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendAsyncCount++;
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(_delayMilliseconds);
                    try
                    {
                        await Task.Delay(_delayMilliseconds, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        throw new TimeoutRejectedException("Simulated request timeout.");
                    }
                }

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
        }
    }
}
