using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Polly;
using Polly.Retry;

namespace CopilotClient.Services;

public sealed class AiRetryPolicy
{
    public AsyncRetryPolicy RetryPolicy { get; }

    public AiRetryPolicy()
    {
        RetryPolicy = Policy
            .Handle<RequestFailedException>(IsTransient)
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                {
                    // Exponential backoff with a bit of jitter
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2, 4, 8...
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                    return baseDelay + jitter;
                },
                onRetry: (exception, delay, attempt, context) =>
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AI Retry] Attempt {attempt}, waiting {delay}. Reason: {exception.Message}");
                }
            );
    }

    private static bool IsTransient(RequestFailedException ex)
    {
        return ex.Status is 408 or 429 or >= 500 and < 600;
    }
}
