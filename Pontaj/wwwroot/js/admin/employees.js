// Employees admin page: client-side list filter + on-demand detail loading.
// The list is rendered server-side (Lists/_EmployeesList); clicking a row
// fetches the detail partial as HTML via apiRequest and injects it.

const APP_EMPLOYEE_DETAIL_URL = '/Admin/EmployeeGeneralInfo';
const APP_EMPLOYEE_SYNC_URL = '/Admin/SyncEmployees';

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('employeeSearch');
    const list = document.getElementById('employeeList');
    const placeholder = document.getElementById('workAreaPlaceholder');
    const details = document.getElementById('employeeDetails');
    const detailsContent = document.getElementById('employeeDetailsContent');
    const selectedName = document.getElementById('selectedEmployeeName');

    let isLoading = false;

    // ---- Client-side filter over the already-rendered rows -----------------
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const filter = normalizeForSearch(searchInput.value.trim());
            const items = list.querySelectorAll('.employee-item');
            items.forEach(function (item) {
                const text = normalizeForSearch(item.textContent);
                if (text.includes(filter)) {
                    item.classList.remove('d-none');
                } else {
                    item.classList.add('d-none');
                }
            });
        });
    }

    // ---- Row click → load the detail partial ------------------------------
    if (list) {
        list.addEventListener('click', function (event) {
            const item = event.target.closest('.employee-item');
            if (!item) {
                return;
            }
            if (isLoading) {
                return;
            }

            const id = item.getAttribute('data-id');
            if (!id) {
                return;
            }

            const nameEl = item.querySelector('b');
            selectedName.textContent = nameEl ? nameEl.textContent : '';

            placeholder.classList.add('d-none');
            details.classList.remove('d-none');

            const activeItems = list.querySelectorAll('.employee-item.active');
            activeItems.forEach(function (el) {
                el.classList.remove('active');
            });
            item.classList.add('active');

            loadEmployeeDetail(id);
        });
    }

    function loadEmployeeDetail(id) {
        isLoading = true;
        document.body.style.cursor = 'wait';
        detailsContent.innerHTML = '<div class="text-muted mt-2">Se încarcă...</div>';

        // success and error are mutually exclusive, so each releases the guard.
        const release = function () {
            isLoading = false;
            document.body.style.cursor = 'default';
        };

        apiRequest({
            method: 'GET',
            path: APP_EMPLOYEE_DETAIL_URL,
            query: { id: id },
            expect: 'html',
            onSuccess: function (html) {
                detailsContent.innerHTML = html;
                release();
            },
            onError: function (err) {
                detailsContent.innerHTML =
                    '<p class="text-danger mt-2">Eroare la încărcarea detaliilor angajatului.</p>';
                if (typeof showToast === 'function') {
                    showToast('error', err && err.message ? err.message : 'Eroare la încărcare.');
                }
                release();
            }
        });
    }

    // ---- Sync button: pull employees from the source, then reload ---------
    const syncButton = document.getElementById('syncEmployees');
    if (syncButton) {
        const syncIcon = syncButton.querySelector('i');

        syncButton.addEventListener('click', function () {
            syncButton.disabled = true;
            if (syncIcon) {
                syncIcon.classList.add('spin');
            }

            const restore = function () {
                syncButton.disabled = false;
                if (syncIcon) {
                    syncIcon.classList.remove('spin');
                }
            };

            apiRequest({
                method: 'POST',
                path: APP_EMPLOYEE_SYNC_URL,
                onSuccess: function () {
                    if (typeof showToast === 'function') {
                        showToast('success', 'Sincronizare finalizată.');
                    }
                    // Reload so the freshly synced list is rendered server-side.
                    window.location.reload();
                },
                onError: function (err) {
                    if (typeof showToast === 'function') {
                        showToast('error', err && err.message ? err.message : 'Eroare la sincronizare.');
                    }
                    restore();
                }
            });
        });
    }
});
