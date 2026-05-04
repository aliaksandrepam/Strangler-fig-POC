import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

export interface TimerPayload {
  isRunning: boolean
  elapsedSeconds: number
}

/**
 * Subscribes to /hubs/timer (proxied via the YARP gateway). Both Tick and
 * StateChanged events carry the same payload shape: { isRunning, elapsedSeconds }.
 * Stopwatch semantics — pausing freezes elapsedSeconds, resuming continues from
 * the same value. Server's TimerState is the single source of truth.
 */
export function useTimer() {
  const [snapshot, setSnapshot] = useState<TimerPayload | null>(null)
  const [status, setStatus] = useState<ConnectionStatus>('disconnected')
  const connRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/timer')
      .withAutomaticReconnect()
      .build()
    connRef.current = conn

    const onPayload = (p: TimerPayload) => setSnapshot(p)
    conn.on('Tick', onPayload)
    conn.on('StateChanged', onPayload)
    conn.onreconnecting(() => setStatus('reconnecting'))
    conn.onreconnected(() => setStatus('connected'))
    conn.onclose(() => setStatus('disconnected'))

    setStatus('connecting')
    conn.start()
      .then(() => setStatus('connected'))
      .catch((err) => {
        console.warn('timer hub connect failed', err)
        setStatus('disconnected')
      })

    return () => {
      if (conn.state !== HubConnectionState.Disconnected) conn.stop().catch(() => {})
    }
  }, [])

  const start = useCallback(async () => {
    const c = connRef.current
    if (!c || c.state !== HubConnectionState.Connected) return
    try { await c.invoke('Start') } catch (e) { console.warn('Start failed', e) }
  }, [])

  const stop = useCallback(async () => {
    const c = connRef.current
    if (!c || c.state !== HubConnectionState.Connected) return
    try { await c.invoke('Stop') } catch (e) { console.warn('Stop failed', e) }
  }, [])

  return {
    snapshot,
    isRunning: snapshot?.isRunning ?? true,
    elapsedSeconds: snapshot?.elapsedSeconds ?? 0,
    status,
    start,
    stop,
  }
}
