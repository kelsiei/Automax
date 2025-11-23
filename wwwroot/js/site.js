// site.js
// TODO: Add shared client-side behavior for CarCareTracker pages.
console.log("CarCareTracker site.js loaded");

(function () {
    var THEME_KEY = 'carcare-theme';

    function applyTheme(theme) {
        var isDark = theme === 'dark';
        document.body.classList.toggle('dark-mode', isDark);

        var toggle = document.getElementById('themeToggle');
        if (toggle) {
            toggle.textContent = isDark ? 'Dark mode' : 'Light mode';
        }
    }

    function initTheme() {
        var stored = null;
        try {
            stored = window.localStorage ? localStorage.getItem(THEME_KEY) : null;
        } catch (e) {
            stored = null;
        }

        var theme = (stored === 'dark' || stored === 'light') ? stored : 'light';
        applyTheme(theme);

        var toggle = document.getElementById('themeToggle');
        if (toggle) {
            toggle.addEventListener('click', function (e) {
                e.preventDefault();
                var current = theme;
                try {
                    if (window.localStorage) {
                        var storedTheme = localStorage.getItem(THEME_KEY);
                        if (storedTheme === 'dark' || storedTheme === 'light') {
                            current = storedTheme;
                        }
                    }
                } catch (err) { }

                var next = current === 'dark' ? 'light' : 'dark';
                try {
                    if (window.localStorage) {
                        localStorage.setItem(THEME_KEY, next);
                    }
                } catch (err) { }

                applyTheme(next);
                theme = next;
            });
        }
    }

    document.addEventListener('DOMContentLoaded', initTheme);
})();
