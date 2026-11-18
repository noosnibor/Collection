/*
 * snapshotPlayer.js - admin side of Snapshot: plays a recorded session with rrweb-player.
 * Expects the globals "rrwebPlayer" (lib/rrweb/rrweb-player.min.js) loaded in App.razor.
 */

let __player = null;
let __dotNet = null;
let __lastTick = 0;
let __skipIdle = true;

/**
 * Builds the player inside `host`.
 * chunks = array of base64 strings, each one gzip(JSON array of rrweb events), already in playback order.
 */
export async function load(host, chunks, dotNetRef) {
    destroy();
    __dotNet = dotNetRef;
    __skipIdle = true;

    const events = [];
    for (const b64 of chunks) {
        const json = await gunzipBase64(b64);
        for (const ev of JSON.parse(json)) events.push(ev);
    }
    if (events.length < 2) return false;

    const Player = window.rrwebPlayer?.default ?? window.rrwebPlayer?.Player ?? window.rrwebPlayer;

    host.innerHTML = "";
    __player = new Player({
        target: host,
        props: {
            events,
            width: host.clientWidth,
            height: Math.max(300, host.clientHeight - 64),   // room for the controller bar under the video
            autoPlay: false,
            skipInactive: true,
            showController: true,
            speedOption: [0.5, 1, 2, 4, 8, 16]
        }
    });

    // Tell Blazor where the playhead is (a few times a second) so the timeline can highlight the current event.
    __player.addEventListener("ui-update-current-time", ev => {
        const now = performance.now();
        if (now - __lastTick < 400) return;
        __lastTick = now;
        __dotNet?.invokeMethodAsync("OnPlayerTick", ev.payload);
    });
    return true;
}

/** Jumps to a time (ms from the start) and plays, starting 1.5 seconds early for context. */
export function seek(offsetMs) {
    __player?.goto(Math.max(0, offsetMs - 1500), true);
}

export function setSpeed(speed) { __player?.setSpeed(speed); }
export function togglePlay() { __player?.toggle(); }

/** rrweb-player only has a toggle, so remember the state and flip it only when it differs. */
export function setSkipIdle(on) {
    if (!__player || on === __skipIdle) return;
    __player.toggleSkipInactive();
    __skipIdle = on;
}

export function destroy() {
    try { __player?.$destroy?.(); } catch { /* ignore */ }
    __player = null;
}

async function gunzipBase64(b64) {
    const binary = atob(b64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);

    const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
    return await new Response(stream).text();
}
