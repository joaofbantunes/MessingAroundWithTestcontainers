namespace Api.Tests;

public static class RetryExtensions
{
    public static async Task<TResult> ExecuteAndRetryAsync<TResult>(
        Func<Task<TResult>> execute,
        Func<TResult, bool> until, 
        TimeSpan every,
        TimeSpan forMaximum)
    {
        var timeProvider = TimeProvider.System;
        var startingTimeStamp = timeProvider.GetTimestamp();
        while (timeProvider.GetElapsedTime(startingTimeStamp) < forMaximum)
        {
            var result = await execute();
            if (until(result))
            {
                return result;
            }
            await Task.Delay(every);
        }
        throw new TimeoutException();
    }
}