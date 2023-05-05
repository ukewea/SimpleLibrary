using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLibrary
{
    public class MyHttpClient
    {
        // Properties
        /// <summary>
        /// Gets or sets the timeout duration in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the function used to calculate the sleep duration between retries.
        /// </summary>
        public Func<int, TimeSpan> SleepDurationProvider { get; set; } = DefaultSleepDurationProvider;

        public HttpMessageHandler Handler { get; set; } = null;

        /// <summary>
        /// Sends an HTTP GET request to the specified URL using retry and timeout policies.
        /// </summary>
        /// <param name="url">The URL to send the request to.</param>
        /// <returns>A Task representing the asynchronous operation, with the resulting HttpResponseMessage.</returns>
        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            var retryPolicy = GetRetryPolicy();
            var timeoutPolicy = GetTimeoutPolicy();

            var policyWrap = Policy.WrapAsync(retryPolicy, timeoutPolicy);

            HttpClient httpClient;
            if (Handler == null)
                httpClient = new HttpClient();
            else
                httpClient = new HttpClient(Handler);

            using (httpClient)
            {
                return await
                    retryPolicy.ExecuteAsync(() =>
                        timeoutPolicy.ExecuteAsync(async token =>
                            await httpClient.GetAsync(url, token), CancellationToken.None));
            }
        }

        /// <summary>
        /// Creates and returns a retry policy for handling transient errors and timeouts.
        /// </summary>
        /// <returns>An IAsyncPolicy instance configured for handling retries.</returns>
        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var jitterier = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles 4xx and 5xx errors
                .Or<TimeoutRejectedException>() // Handles timeouts
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => SleepDurationProvider(retryAttempt),
                    onRetry: (response, timeSpan, retryCount, context) =>
                    {
                        Debug.WriteLine($"Request failed. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                    }
                );
        }

        /// <summary>
        /// Creates and returns a timeout policy with the configured timeout duration.
        /// </summary>
        /// <returns>An IAsyncPolicy instance configured for handling timeouts.</returns>
        private IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeoutSeconds);
        }

        /// <summary>
        /// Default function used to calculate the sleep duration between retries based on the retry attempt number.
        /// </summary>
        /// <param name="retryAttempt">The current retry attempt number.</param>
        /// <returns>A TimeSpan representing the sleep duration for the current retry attempt.</returns>
        private static TimeSpan DefaultSleepDurationProvider(int retryAttempt)
        {
            var jitterier = new Random();
            return TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) +
                TimeSpan.FromMilliseconds(jitterier.Next(0, 20));
        }
    }
}
