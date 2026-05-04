// =============================================================================
// Toast notifications.
// Reads/writes the Bootstrap toast structure rendered in _Layout.cshtml:
//   #alert (root), #toastHeader, #toastTitle, #toastCreationMoment,
//   #toastContentHTML (iframe, used for HTML content), #toastContent (div, plain).
// =============================================================================

function showToast(opType, content) {
    let title = '';
    let iconClass = '';
    let colorClass = '';

    switch (opType) {
        case 'error':
            title = 'Eroare.';
            iconClass = 'bi bi-x-circle';
            colorClass = 'text-danger';
            break;
        case 'success':
            title = 'Succes.';
            iconClass = 'bi bi-check2-circle';
            colorClass = 'text-success';
            break;
        case 'warning':
            title = 'Atenție!';
            iconClass = 'bi bi-exclamation-triangle';
            colorClass = 'text-warning';
            break;
        case 'information':
            title = 'Informație:';
            iconClass = 'bi bi-info-circle';
            colorClass = 'text-info';
            break;
        default:
            title = 'Informație:';
            iconClass = 'bi bi-info-circle';
            colorClass = 'text-secondary';
            break;
    }

    const toastEl = document.getElementById('alert');
    const headerEl = document.getElementById('toastHeader');
    const titleEl = document.getElementById('toastTitle');
    const momentEl = document.getElementById('toastCreationMoment');
    const htmlEl = document.getElementById('toastContentHTML');
    const plainEl = document.getElementById('toastContent');

    if (!toastEl || !headerEl || !titleEl || !momentEl || !htmlEl || !plainEl) {
        return;
    }

    const iconEl = headerEl.querySelector('i');
    if (iconEl) {
        iconEl.className = iconClass + ' ' + colorClass;
    }

    titleEl.textContent = title;
    titleEl.classList.remove('text-success', 'text-warning', 'text-danger', 'text-info', 'text-secondary');
    titleEl.classList.add(colorClass);

    // Timestamp like "29 apr. 14:32"
    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const months = ['ian.', 'feb.', 'mar.', 'apr.', 'mai', 'iun.', 'iul.', 'aug.', 'sep.', 'oct.', 'noi.', 'dec.'];
    const month = months[now.getMonth()];
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    momentEl.textContent = day + ' ' + month + ' ' + hours + ':' + minutes;

    // Detect HTML so the iframe path is used (sandboxed render of server-issued HTML)
    const isHTML = /<\/?[a-z][\s\S]*>/i.test(content);

    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = content;
    const plainText = tempDiv.textContent || tempDiv.innerText || '';
    const wordCount = plainText.trim().split(/\s+/).filter(Boolean).length;

    // Autohide delay: ~200ms/word, clamped to [3s, 15s]
    const delay = Math.min(Math.max(3000, wordCount * 200), 15000);

    toastEl.style.width = '31.25rem';

    if (isHTML) {
        htmlEl.classList.remove('d-none');
        plainEl.classList.add('d-none');
        toastEl.style.height = 'fit-content';
        htmlEl.style.maxHeight = '18rem';
        htmlEl.style.overflowY = 'auto';
        htmlEl.style.height = 'calc(100% - ' + (headerEl.offsetHeight || 60) + 'px)';
        htmlEl.onload = function () {
            htmlEl.style.height = (htmlEl.contentWindow.document.body.scrollHeight + 37) + 'px';
        };
        htmlEl.srcdoc = content;
    } else {
        plainEl.classList.remove('d-none');
        htmlEl.classList.add('d-none');
        plainEl.innerText = content;
        plainEl.style.overflowY = 'auto';
        plainEl.style.height = 'auto';
        plainEl.style.maxHeight = '9.375rem';
        toastEl.style.height = 'auto';
    }

    new bootstrap.Toast(toastEl, {
        autohide: true,
        delay: delay
    }).show();
}

document.addEventListener('DOMContentLoaded', function () {
    const logoutBtn = document.getElementById('btn-logout');

    if (logoutBtn) {
        logoutBtn.addEventListener('click', function (e) {
            e.preventDefault();

            apiRequest({
                method: 'POST',
                path: '/api/account/logout',
                onSuccess: function () {

                    localStorage.clear();
                    sessionStorage.clear();
                    window.location.href = '/Account/Login';
                },
                onError: function (err) {

                    localStorage.clear();
                    window.location.href = '/Account/Login';
                }
            });
        });
    }
});
