using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PocApp.Hubs;

/// <summary>
/// Authenticated SignalR hub. Clients receive Tick events (1Hz, only while running)
/// and StateChanged events (when any user toggles Start/Stop). Both events carry
/// the same payload shape: { isRunning, elapsedSeconds }, so a client only needs
/// one render path.
/// </summary>
[Authorize]
public class TimerHub : Hub
{
    private readonly TimerState _state;
    private readonly ILogger<TimerHub> _log;

    public TimerHub(TimerState state, ILogger<TimerHub> log)
    {
        _state = state;
        _log = log;
    }

    public override async Task OnConnectedAsync()
    {
        // Snapshot the current state to the joining client so its UI is correct
        // immediately, even if the timer is currently paused (no Tick coming).
        await Clients.Caller.SendAsync("StateChanged", BuildPayload());
        await base.OnConnectedAsync();
    }

    public async Task Start()
    {
        if (_state.TryResume())
        {
            _log.LogInformation("Timer resumed by {User} at {Elapsed}s", Context.User?.Identity?.Name, _state.ElapsedSeconds);
            await Clients.All.SendAsync("StateChanged", BuildPayload());
        }
    }

    public async Task Stop()
    {
        if (_state.TryPause())
        {
            _log.LogInformation("Timer paused by {User} at {Elapsed}s", Context.User?.Identity?.Name, _state.ElapsedSeconds);
            await Clients.All.SendAsync("StateChanged", BuildPayload());
        }
    }

    private object BuildPayload() => new
    {
        isRunning = _state.IsRunning,
        elapsedSeconds = _state.ElapsedSeconds,
    };
}
