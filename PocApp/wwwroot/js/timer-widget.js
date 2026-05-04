// Connects to /hubs/timer and renders a shared stopwatch into a .timer-widget element.
// Tick + StateChanged events both carry { isRunning, elapsedSeconds }. When paused,
// the server stops broadcasting Ticks and the displayed value freezes; resuming
// restarts broadcasting from the SAME elapsedSeconds (no time skipped over).
(function () {
    function formatElapsed(s) {
        s = Number(s) || 0;
        const h = Math.floor(s / 3600);
        const m = Math.floor((s % 3600) / 60);
        const sec = s % 60;
        const pad = (n) => (n < 10 ? '0' + n : '' + n);
        if (h > 0) return h + ':' + pad(m) + ':' + pad(sec);
        return pad(m) + ':' + pad(sec);
    }

    function start() {
        const widget = document.querySelector('.timer-widget');
        const toggle = document.querySelector('.timer-toggle');
        if (!widget) return;
        const clockEl = widget.querySelector('.timer-clock');
        const uptimeEl = widget.querySelector('.timer-uptime');

        let isRunning = true;
        let connected = false;

        function setConnStatus(name) {
            widget.classList.remove('is-connected', 'is-reconnecting', 'is-disconnected');
            widget.classList.add('is-' + name);
            connected = (name === 'connected');
            updateButton();
        }
        function setRunning(running) {
            isRunning = running;
            widget.classList.toggle('is-paused', !running);
            updateButton();
        }
        function updateButton() {
            if (!toggle) return;
            toggle.classList.toggle('is-running', isRunning);
            toggle.classList.toggle('is-stopped', !isRunning);
            toggle.textContent = isRunning ? 'Stop' : 'Start';
            toggle.disabled = !connected;
            toggle.title = isRunning ? 'Pause the shared stopwatch' : 'Resume the shared stopwatch';
        }
        function applyPayload(payload) {
            // Ticks come at 1Hz while running; StateChanged is fired on toggle and on connect.
            if (typeof payload.elapsedSeconds === 'number') {
                clockEl.textContent = formatElapsed(payload.elapsedSeconds);
                uptimeEl.textContent = '';
                widget.title = 'Stopwatch: ' + formatElapsed(payload.elapsedSeconds);
            }
            if (typeof payload.isRunning === 'boolean') {
                setRunning(payload.isRunning);
            }
        }
        setConnStatus('disconnected');
        setRunning(true);

        const conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/timer')
            .withAutomaticReconnect()
            .build();

        conn.on('Tick', applyPayload);
        conn.on('StateChanged', applyPayload);
        conn.onreconnecting(function () { setConnStatus('reconnecting'); });
        conn.onreconnected(function () { setConnStatus('connected'); });
        conn.onclose(function () { setConnStatus('disconnected'); });

        if (toggle) {
            toggle.addEventListener('click', function () {
                if (!connected) return;
                toggle.disabled = true; // re-enabled by StateChanged
                const method = isRunning ? 'Stop' : 'Start';
                conn.invoke(method).catch(function (err) {
                    console.warn('timer ' + method + ' failed', err);
                    toggle.disabled = false;
                });
            });
        }

        conn.start()
            .then(function () { setConnStatus('connected'); })
            .catch(function (err) { console.warn('timer hub connect failed', err); });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
