using Microsoft.AspNetCore.SignalR;

namespace PocApp.Hubs;

/// <summary>
/// Broadcasts Tick events at 1Hz, but only while TimerState is in the running
/// state. Each tick carries { isRunning: true, elapsedSeconds }; clients render
/// elapsedSeconds as the stopwatch display, so pausing freezes the displayed
/// value (no broadcasts → no updates) and resuming continues from the same number.
/// </summary>
public class TimerBroadcastService : BackgroundService
{
    private readonly IHubContext<TimerHub> _hub;
    private readonly TimerState _state;
    private readonly ILogger<TimerBroadcastService> _log;

    public TimerBroadcastService(IHubContext<TimerHub> hub, TimerState state, ILogger<TimerBroadcastService> log)
    {
        _hub = hub;
        _state = state;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("TimerBroadcastService starting. Tick rate: 1Hz while running.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Skip the broadcast while paused. Clients keep their last rendered
            // value because elapsedSeconds doesn't advance during a pause window.
            if (!_state.IsRunning) continue;

            try
            {
                await _hub.Clients.All.SendAsync("Tick", new
                {
                    isRunning = true,
                    elapsedSeconds = _state.ElapsedSeconds,
                }, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Tick broadcast failed");
            }
        }
    }
}
