// Small enhancement: preview a chosen image before it is uploaded.
document.querySelectorAll('input[type="file"][accept^="image"]').forEach(function (input) {
    input.addEventListener('change', function () {
        var file = input.files && input.files[0];
        if (!file) return;
        if (file.size > 5 * 1024 * 1024) {
            alert('That image is larger than 5 MB. Please choose a smaller one.');
            input.value = '';
        }
    });
});
