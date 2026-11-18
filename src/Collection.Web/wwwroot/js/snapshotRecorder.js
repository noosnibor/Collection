/*
 * snapshotRecorder.js - records what a signed-in user does (Snapshot feature).
 * ------------------------------------------------------------------
 * Real-world behaviour:
 *   - Uses rrweb (loaded in App.razor as the global "rrweb") to record the page as small DOM events, not video.
 *   - Every few seconds the events are gzip-compressed in the browser and handed to the Blazor component
 *     (SnapshotRecorder.razor), which saves them to the database as the signed-in user.
 *   - Typed text is NEVER recorded: every input is masked. Table cell text is masked too by default
 *     (maskTextSelector), because the portal shows member and giving data in tables.
 *   - Add class "sn-block" to hide an element completely, "sn-mask" to hide only its text.
 *   - Add data-snapshot-label="Save title" to a button to give it a friendly name on the admin timeline.
 */

let __dotNet = null;
let __sessionKey = "";
let __seq = 0;
let __buffer = [];
let __markers = [];
let __stopRecording = null;
let __flushTimer = null;
let __sendChain = Promise.resolve();   // keeps chunks in order even when the network is slow
let __failures = 0;

// ---------------------------------------------------------------- public API (called from Blazor)

/** Starts recording. options = { maskTextSelector, flushMs, checkoutMs } */
export async function start(dotNetRef, options) {
    if (__stopRecording) return;
    if (typeof rrweb === "undefined" || typeof CompressionStream === "undefined") return; // old browser: do nothing

    __dotNet = dotNetRef;
    const opts = options || {};

    __sessionKey = await __dotNet.invokeMethodAsync(
        "StartSession",
        location.pathname + location.search,
        window.innerWidth,
        window.innerHeight,
        navigator.userAgent || "");

    if (!__sessionKey) return;   // the database said no (or is down): stay off

    __stopRecording = rrweb.record({
        emit(event) { __buffer.push(event); },
        maskAllInputs: true,                                   // typed text is never captured
        maskTextSelector: opts.maskTextSelector || null,       // e.g. "td, .sn-mask"
        blockClass: "sn-block",
        maskTextClass: "sn-mask",
        blockSelector: ".sn-block, canvas, iframe, embed, object",   // PDFs / documents / canvases are never recorded
        slimDOMOptions: "all",                                 // drop comments, scripts, whitespace: much smaller
        sampling: { mousemove: 150, scroll: 200, input: "last" },
        checkoutEveryNms: opts.checkoutMs || 300000            // fresh full snapshot every 5 min so playback can seek
    });

    document.addEventListener("click", onClick, true);
    window.addEventListener("error", e => addMarker("error", e.message || "Script error"));
    document.addEventListener("visibilitychange", () => { if (document.visibilityState === "hidden") flush(); });
    window.addEventListener("pagehide", () => flush());
    __flushTimer = setInterval(flush, opts.flushMs || 5000);
}

/** Adds a "page change" marker to the timeline. */
export function markNavigation(url) {
    addMarker("navigate", url);
}

/** Stops recording and sends what is left. */
export async function stop() {
    clearInterval(__flushTimer);
    document.removeEventListener("click", onClick, true);
    flush();
    try { await __sendChain; } catch { /* ignore */ }
    if (__stopRecording) { __stopRecording(); __stopRecording = null; }
}

// ---------------------------------------------------------------- internals

function onClick(e) {
    const target = e.target instanceof Element ? e.target : null;
    if (!target) return;

    const el = target.closest("[data-snapshot-label],button,a,[role=button],input,select,textarea,label,tr,li") || target;
    let label = el.closest("[data-snapshot-label]")?.getAttribute("data-snapshot-label");

    if (!label) {
        const tag = el.tagName.toLowerCase();
        if (tag === "input" || tag === "textarea" || tag === "select") {
            // Which field was clicked - never what was typed.
            label = "Field: " + (el.getAttribute("aria-label") || el.name || el.id || tag);
        } else if (el.closest(".sn-block, .sn-mask, td")) {
            label = "Click (hidden area)";
        } else {
            label = (el.innerText || el.getAttribute("aria-label") || el.title || tag).trim().replace(/\s+/g, " ");
        }
    }
    addMarker("click", label.slice(0, 60));
}

function addMarker(kind, label) {
    __markers.push({
        flngTimestampMs: Date.now(),
        fstrKind: kind,
        fstrLabel: label,
        fstrUrl: location.pathname
    });
}

/** Takes what is buffered and queues it for sending. */
function flush() {
    // Markers wait for the next batch that has recording events (the database stores both together).
    if (!__sessionKey || __buffer.length === 0) return;

    const events = __buffer;
    const markers = __markers;
    __buffer = [];
    __markers = [];
    const sequence = ++__seq;

    __sendChain = __sendChain.then(() => send(sequence, events, markers)).catch(() => { });
}

async function send(sequence, events, markers) {
    if (!__dotNet) return;

    const first = events.length ? events[0].timestamp : Date.now();
    const last = events.length ? events[events.length - 1].timestamp : Date.now();
    const data = events.length ? await gzipBase64(JSON.stringify(events)) : "";

    let ok = false;
    try {
        ok = await __dotNet.invokeMethodAsync("SendChunk", __sessionKey, sequence, events.length, first, last, data, markers);
    } catch { ok = false; }

    if (ok) { __failures = 0; return; }

    // The database said no. After a few failures in a row stop recording instead of retrying forever.
    if (++__failures >= 5) {
        clearInterval(__flushTimer);
        if (__stopRecording) { __stopRecording(); __stopRecording = null; }
    }
}

/** gzip + base64 using the browser's built-in CompressionStream. */
async function gzipBase64(text) {
    const stream = new Blob([text]).stream().pipeThrough(new CompressionStream("gzip"));
    const bytes = new Uint8Array(await new Response(stream).arrayBuffer());

    let binary = "";
    const step = 0x8000;   // build the string in pieces so very large arrays don't overflow the call stack
    for (let i = 0; i < bytes.length; i += step) {
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + step));
    }
    return btoa(binary);
}
