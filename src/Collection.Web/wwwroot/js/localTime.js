// Reports the viewer's time zone to the server so stored UTC times can be shown in local time.
window.portalLocalTime = {
    // IANA id such as "America/Jamaica", "America/Toronto" or "Europe/London" ("" when unavailable)
    zone: function () {
        try { return Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch (e) { return ""; }
    },
    // Minutes to ADD to UTC to get local time right now (used only if the IANA id is unknown to the server)
    offsetMinutes: function () { return -new Date().getTimezoneOffset(); }
};
