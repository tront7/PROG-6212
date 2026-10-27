// RaceDay front-end helpers (kept deliberately small: Bootstrap does the heavy lifting).
(function () {
    // Live preview for any image <input type="file" data-preview="#elementId">.
    document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
        input.addEventListener('change', function () {
            var target = document.querySelector(input.getAttribute('data-preview'));
            if (!target) return;
            var file = input.files && input.files[0];
            if (!file) return;
            target.src = URL.createObjectURL(file);
            target.classList.remove('d-none');
        });
    });

    // Stop double submissions: disable the submit button once a form is posted.
    document.querySelectorAll('form[data-once]').forEach(function (form) {
        form.addEventListener('submit', function () {
            var btn = form.querySelector('button[type="submit"]');
            if (btn) {
                btn.disabled = true;
                btn.dataset.label = btn.textContent;
                btn.textContent = 'Please wait…';
            }
        });
    });
})();
