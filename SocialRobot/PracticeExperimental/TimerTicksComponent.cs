using System;
using System.Threading;
using System.Threading.Tasks;

public class TimerTicksComponent
{
    public async Task StartTimer(int durationInTicks, Action onTimerComplete)
    {
        // Using Task.Delay for a non-blocking wait
        await Task.Delay(TimeSpan.FromTicks(durationInTicks));

        // Invoke the callback action when the timer is done
        onTimerComplete?.Invoke();
    }
}