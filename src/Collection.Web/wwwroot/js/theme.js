/* ============================================================================
   Portal theme manager (Windows 11 style light / dark).
   - Stores the user's choice ("system" | "light" | "dark") in localStorage.
   - Applies the RESOLVED theme to <html data-theme="..." data-bs-theme="...">.
   - Follows the operating-system setting live while the choice is "system".
   This script is loaded in <head> so the theme is applied before first paint
   (no white flash in dark mode).
   ========================================================================== */
(function () {
    'use strict';

    var KEY = 'portal-theme';
    var media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function read() {
        try { var v = localStorage.getItem(KEY); return (v === 'light' || v === 'dark') ? v : 'system'; }
        catch (e) { return 'system'; }
    }

    function resolve(mode) {
        return mode === 'system' ? (media && media.matches ? 'dark' : 'light') : mode;
    }

    function apply(mode) {
        var theme = resolve(mode);
        var root = document.documentElement;
        root.setAttribute('data-theme', theme);
        root.setAttribute('data-bs-theme', theme);
        root.setAttribute('data-theme-mode', mode);
        return theme;
    }

    window.portalTheme = {
        /** Returns the saved choice: "system" | "light" | "dark". */
        getMode: function () { return read(); },
        /** Returns the theme actually shown: "light" | "dark". */
        getResolved: function () { return resolve(read()); },
        /** Saves and applies a choice. Returns the resolved theme. */
        setMode: function (mode) {
            try {
                if (mode === 'light' || mode === 'dark') localStorage.setItem(KEY, mode);
                else localStorage.removeItem(KEY);
            } catch (e) { /* storage blocked – still apply for this page */ }
            return apply(mode);
        },
        /** Removes every stored value EXCEPT the theme choice (used when the app version changes). */
        clearStorageKeepTheme: function () {
            var keep = read();
            try { localStorage.clear(); } catch (e) { }
            this.setMode(keep);
        }
    };

    apply(read());

    // Follow OS changes while in "system" mode.
    if (media && media.addEventListener) {
        media.addEventListener('change', function () { if (read() === 'system') apply('system'); });
    }
})();
