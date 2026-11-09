// Small helpers for the RaceDay site.
(function () {
    // Register page: show the fields that match the chosen role.
    function applyRole() {
        var chosen = document.querySelector('[data-role-toggle]:checked');
        var participant = document.getElementById('participantFields');
        var organiser = document.getElementById('organiserFields');
        if (!chosen || !participant || !organiser) return;
        var isOrganiser = chosen.value === 'Organiser';
        participant.style.display = isOrganiser ? 'none' : '';
        organiser.style.display = isOrganiser ? '' : 'none';
    }
    document.querySelectorAll('[data-role-toggle]').forEach(function (r) {
        r.addEventListener('change', applyRole);
    });
    applyRole();

    // Ask before deleting.
    document.querySelectorAll('form[data-confirm]').forEach(function (f) {
        f.addEventListener('submit', function (e) {
            if (!window.confirm(f.getAttribute('data-confirm'))) e.preventDefault();
        });
    });

    // Event picker: submit as soon as another event is chosen.
    document.querySelectorAll('select[data-autosubmit]').forEach(function (s) {
        s.addEventListener('change', function () { s.form.submit(); });
    });
})();
