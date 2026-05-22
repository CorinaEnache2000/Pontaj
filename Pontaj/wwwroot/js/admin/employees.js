const APP_EMPLOYEE_DETAIL_URL = '/Admin/EmployeeGeneralInfo';
const APP_EMPLOYEE_SYNC_URL = '/Admin/SyncEmployees';
const APP_EMPLOYEE_SET_ACTIVE_URL = '/Admin/SetEmployeeActive';

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('employeeSearch');
    const list = document.getElementById('employeeList');
    const placeholder = document.getElementById('workAreaPlaceholder');
    const details = document.getElementById('employeeDetails');
    const detailsContent = document.getElementById('employeeDetailsContent');
    const selectedName = document.getElementById('selectedEmployeeName');

    let isLoading = false;
    let selectedEmployee = null;

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

            selectedEmployee = {
                id: parseInt(id, 10),
                active: item.getAttribute('data-active') === 'true'
            };
            updateToggleActiveButton();

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

    const toggleActiveButton = document.getElementById('employeeToggleActiveButton');
    const toggleActiveLabel = document.getElementById('employeeToggleActiveLabel');

    function updateToggleActiveButton() {
        if (!toggleActiveButton || !toggleActiveLabel || !selectedEmployee) {
            return;
        }
        if (selectedEmployee.active) {
            toggleActiveLabel.textContent = 'Dezactivează';
            toggleActiveButton.classList.remove('btn-success');
            toggleActiveButton.classList.add('btn-danger');
        } else {
            toggleActiveLabel.textContent = 'Activează';
            toggleActiveButton.classList.remove('btn-danger');
            toggleActiveButton.classList.add('btn-success');
        }
    }

    if (toggleActiveButton) {
        toggleActiveButton.addEventListener('click', function () {
            if (!selectedEmployee) {
                return;
            }
            const targetActive = !selectedEmployee.active;
            toggleActiveButton.disabled = true;

            apiRequest({
                method: 'POST',
                path: APP_EMPLOYEE_SET_ACTIVE_URL,
                body: { id: selectedEmployee.id, active: targetActive },
                onSuccess: function () {
                    queueToast('success', targetActive ? 'Angajat activat.' : 'Angajat dezactivat.');
                    reloadKeepingSelection(selectedEmployee.id);
                },
                onError: function (err) {
                    showToast('error', err && err.message ? err.message : 'Eroare la modificare.');
                    toggleActiveButton.disabled = false;
                }
            });
        });
    }

    (function autoSelectFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const selectedId = params.get('selected');
        if (!selectedId) {
            return;
        }
        const item = list ? list.querySelector(
            '.employee-item[data-id="' + selectedId.replace(/"/g, '') + '"]') : null;
        if (item) {
            const cleanUrl = window.location.pathname + window.location.hash;
            window.history.replaceState({}, document.title, cleanUrl);
            item.scrollIntoView({ block: 'nearest' });
            item.click();
        }
    })();

    function loadEmployeeDetail(id) {
        isLoading = true;
        document.body.style.cursor = 'wait';
        detailsContent.innerHTML = '<div class="text-muted mt-2">Se încarcă...</div>';

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
