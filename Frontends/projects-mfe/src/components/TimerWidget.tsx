import { useState } from 'react'
import { useTimer } from '../realtime/useTimer'

function formatElapsed(s: number): string {
  s = Math.max(0, Math.floor(s))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  const pad = (n: number) => (n < 10 ? '0' + n : '' + n)
  if (h > 0) return `${h}:${pad(m)}:${pad(sec)}`
  return `${pad(m)}:${pad(sec)}`
}

/**
 * Visual twin of PocApp/wwwroot/js/timer-widget.js. Renders the shared stopwatch
 * fed by /hubs/timer. Pausing freezes the displayed elapsed value; resuming
 * continues from the same value. State is synchronized across all clients.
 */
export default function TimerWidget() {
  const { isRunning, elapsedSeconds, status, start, stop, snapshot } = useTimer()
  const [pending, setPending] = useState(false)

  const connected = status === 'connected'
  const widgetCls =
    `timer-widget is-${status === 'connecting' ? 'disconnected' : status}` +
    (isRunning ? '' : ' is-paused')
  const display = snapshot ? formatElapsed(elapsedSeconds) : '00:00'
  const tooltip = snapshot ? `Stopwatch: ${formatElapsed(elapsedSeconds)}` : 'connecting…'

  const onToggle = async () => {
    if (!connected || pending) return
    setPending(true)
    try {
      if (isRunning) await stop()
      else await start()
    } finally {
      setPending(false)
    }
  }

  return (
    <>
      <div className={widgetCls} title={tooltip} aria-live="polite">
        <span className="conn-dot" aria-hidden="true"></span>
        <span className="timer-clock">{display}</span>
        <span className="timer-uptime"></span>
      </div>
      <button
        type="button"
        className={`timer-toggle ${isRunning ? 'is-running' : 'is-stopped'}`}
        disabled={!connected || pending}
        title={isRunning ? 'Pause the shared stopwatch' : 'Resume the shared stopwatch'}
        onClick={onToggle}
      >
        {isRunning ? 'Stop' : 'Start'}
      </button>
    </>
  )
}
