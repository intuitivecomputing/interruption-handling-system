using System;
using System.Threading;
using System.Threading.Tasks;

public class TimerSecondsComponent
{
    public async Task StartTimer(float durationInSeconds, Action onTimerComplete, CancellationToken cancellationToken)
    {
        try
        {
            // Task.Delay now respects the CancellationToken
            await Task.Delay(TimeSpan.FromSeconds(durationInSeconds), cancellationToken);

            // Check if the cancellation was requested before invoking the callback
            if (!cancellationToken.IsCancellationRequested)
            {
                onTimerComplete?.Invoke();
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("The timer was cancelled.");
        }
    }
}