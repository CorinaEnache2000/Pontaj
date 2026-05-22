const APP_WORKSTATION_DETAIL_URL = '/Admin/WorkStationGeneralInfo';
const APP_WORKSTATION_CREATE_URL = '/Admin/CreateWorkStation';
const APP_WORKSTATION_UPDATE_URL = '/Admin/UpdateWorkStation';
const APP_WORKSTATION_SET_ACTIVE_URL = '/Admin/SetWorkStationActive';

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('workStationSearch');
    const list = document.getElementById('workStationList');
    const placeholder = document.getElementById('workStationWorkAreaPlaceholder');
    const details = document.getElementById('workStationDetails');
    const detailsContent = document.getElementById('workStationDetailsContent');
    const selectedName = document.getElementById('selectedWorkStationName');

    let activeFilters = {
        ouIds: [],
        ip: '',
        name: ''
    };

    let isLoading = false;
    let selectedWorkStation = null;

    attachSelectSearch(document.getElementById('filterOu'), 'Introduceți unitatea');
    attachSelectSearch(document.getElementById('addOu'), 'Introduceți unitatea');
    attachSelectSearch(document.getElementById('editOu'), 'Introduceți unitatea');

    function getItems() {
        return list ? list.querySelectorAll('.workstation-item') : [];
    }

    function applyFilters() {
        const term = normalizeForSearch(searchInput ? searchInput.value.trim() : '');
        const filterIp = normalizeForSearch(activeFilters.ip);
        const filterName = normalizeForSearch(activeFilters.name);
        const filterOuIds = activeFilters.ouIds;

        getItems().forEach(function (item) {
            const itemName = normalizeForSearch(item.getAttribute('data-name') || '');
            const itemIp = normalizeForSearch(item.getAttribute('data-ip') || '');
            const itemOuId = item.getAttribute('data-ou-id') || '';
            const itemText = normalizeForSearch(item.textContent);

            const matchesSearch = term === '' || itemText.includes(term);
            const matchesOu = filterOuIds.length === 0 || filterOuIds.indexOf(itemOuId) !== -1;
            const matchesIp = filterIp === '' || itemIp.includes(filterIp);
            const matchesName = filterName === '' || itemName.includes(filterName);

            if (matchesSearch && matchesOu && matchesIp && matchesName) {
                item.classList.remove('d-none');
            } else {
                item.classList.add('d-none');
            }
        });

        renderActiveFiltersBadge();
    }

    function renderActiveFiltersBadge() {
        const container = document.getElementById('workStationActiveFilters');
        const badge = document.getElementById('workStationActiveFiltersBadge');
        if (!container || !badge) {
            return;
        }

        badge.textContent = '';

        function appendChip(text) {
            const chip = document.createElement('span');
            chip.className = 'badge bg-info text-dark text-wrap text-start mw-100';
            chip.textContent = text;
            badge.appendChild(chip);
        }

        if (activeFilters.ouIds.length > 0) {
            const ouSelect = document.getElementById('filterOu');
            activeFilters.ouIds.forEach(function (id) {
                const option = ouSelect
                    ? ouSelect.querySelector('option[value="' + id + '"]')
                    : null;
                appendChip('Unitate: ' + (option ? option.textContent : id));
            });
        }
        if (activeFilters.ip) {
            appendChip('IP: ' + activeFilters.ip);
        }
        if (activeFilters.name) {
            appendChip('Nume: ' + activeFilters.name);
        }

        if (badge.children.length === 0) {
            container.classList.add('d-none');
        } else {
            container.classList.remove('d-none');
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', applyFilters);
    }

    const filterApply = document.getElementById('filterApplyButton');
    const filterReset = document.getElementById('filterResetButton');
    const filterClear = document.getElementById('workStationClearFilters');

    if (filterApply) {
        filterApply.addEventListener('click', function () {
            const ouSelect = document.getElementById('filterOu');
            const ipInput = document.getElementById('filterIp');
            const nameInput = document.getElementById('filterName');
            const ouIds = ouSelect
                ? Array.from(ouSelect.selectedOptions)
                    .map(function (o) { return o.value; })
                    .filter(function (v) { return v !== ''; })
                : [];
            activeFilters = {
                ouIds: ouIds,
                ip: ipInput ? ipInput.value.trim() : '',
                name: nameInput ? nameInput.value.trim() : ''
            };
            applyFilters();
        });
    }

    function resetSelectValue(el) {
        if (!el) { return; }
        if (el.multiple) {
            Array.from(el.options).forEach(function (o) { o.selected = false; });
        } else {
            el.value = '';
        }
        el.dispatchEvent(new Event('change', { bubbles: true }));
    }

    if (filterReset) {
        filterReset.addEventListener('click', function () {
            resetSelectValue(document.getElementById('filterOu'));
            const ipInput = document.getElementById('filterIp');
            const nameInput = document.getElementById('filterName');
            if (ipInput) { ipInput.value = ''; }
            if (nameInput) { nameInput.value = ''; }
            activeFilters = { ouIds: [], ip: '', name: '' };
            applyFilters();
        });
    }

    if (filterClear) {
        filterClear.addEventListener('click', function () {
            resetSelectValue(document.getElementById('filterOu'));
            const ipInput = document.getElementById('filterIp');
            const nameInput = document.getElementById('filterName');
            if (ipInput) { ipInput.value = ''; }
            if (nameInput) { nameInput.value = ''; }
            activeFilters = { ouIds: [], ip: '', name: '' };
            applyFilters();
        });
    }

    if (list) {
        list.addEventListener('click', function (event) {
            const item = event.target.closest('.workstation-item');
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

            selectedWorkStation = {
                id: parseInt(id, 10),
                name: item.getAttribute('data-name') || '',
                ip: item.getAttribute('data-ip') || '',
                ouId: item.getAttribute('data-ou-id') || '',
                active: !item.classList.contains('fst-italic')
            };
            updateToggleActiveButton();

            placeholder.classList.add('d-none');
            details.classList.remove('d-none');

            const activeItems = list.querySelectorAll('.workstation-item.active');
            activeItems.forEach(function (el) {
                el.classList.remove('active');
            });
            item.classList.add('active');

            loadDetail(id);
        });
    }

    function loadDetail(id) {
        isLoading = true;
        document.body.style.cursor = 'wait';
        detailsContent.innerHTML = '<div class="text-muted mt-2">Se încarcă...</div>';

        const release = function () {
            isLoading = false;
            document.body.style.cursor = 'default';
        };

        apiRequest({
            method: 'GET',
            path: APP_WORKSTATION_DETAIL_URL,
            query: { id: id },
            expect: 'html',
            onSuccess: function (html) {
                detailsContent.innerHTML = html;
                release();
            },
            onError: function (err) {
                detailsContent.innerHTML =
                    '<p class="text-danger mt-2">Eroare la încărcarea detaliilor stației.</p>';
                if (typeof showToast === 'function') {
                    showToast('error', err && err.message ? err.message : 'Eroare la încărcare.');
                }
                release();
            }
        });
    }

    const editButton = document.getElementById('workStationEditButton');
    const editSubmit = document.getElementById('editSubmitButton');
    const editSpinner = document.getElementById('editSubmitSpinner');

    if (editButton) {
        editButton.addEventListener('click', function () {
            if (!selectedWorkStation) {
                return;
            }
            document.getElementById('editId').value = selectedWorkStation.id;
            document.getElementById('editName').value = selectedWorkStation.name;
            document.getElementById('editIp').value = selectedWorkStation.ip;
            const editOu = document.getElementById('editOu');
            editOu.value = selectedWorkStation.ouId;
            editOu.dispatchEvent(new Event('change', { bubbles: true }));
            document.getElementById('editActive').checked = selectedWorkStation.active;

            const modalEl = document.getElementById('workStationEditModal');
            if (modalEl) {
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            }
        });
    }

    if (editSubmit) {
        editSubmit.addEventListener('click', function () {
            const id = parseInt(document.getElementById('editId').value, 10);
            const name = document.getElementById('editName').value.trim();
            const ip = document.getElementById('editIp').value.trim();
            const ouValue = document.getElementById('editOu').value;
            const active = document.getElementById('editActive').checked;

            if (!id) {
                showToast('error', 'Stația de lucru nu este selectată.');
                return;
            }
            if (!name) {
                showToast('error', 'Numele stației este obligatoriu.');
                document.getElementById('editName').focus();
                return;
            }
            if (!ouValue) {
                showToast('error', 'Selectați unitatea organizațională.');
                document.getElementById('editOu').focus();
                return;
            }

            editSubmit.disabled = true;
            if (editSpinner) { editSpinner.classList.remove('d-none'); }

            const restore = function () {
                editSubmit.disabled = false;
                if (editSpinner) { editSpinner.classList.add('d-none'); }
            };

            apiRequest({
                method: 'POST',
                path: APP_WORKSTATION_UPDATE_URL,
                body: {
                    id: id,
                    name: name,
                    ip: ip === '' ? null : ip,
                    organizationalUnitId: parseInt(ouValue, 10),
                    active: active
                },
                onSuccess: function () {
                    queueToast('success', 'Stație de lucru actualizată.');
                    reloadKeepingSelection(id);
                },
                onError: function (err) {
                    showToast('error', err && err.message ? err.message : 'Eroare la actualizare.');
                    restore();
                }
            });
        });
    }

    const toggleActiveButton = document.getElementById('workStationToggleActiveButton');
    const toggleActiveLabel = document.getElementById('workStationToggleActiveLabel');

    function updateToggleActiveButton() {
        if (!toggleActiveButton || !toggleActiveLabel || !selectedWorkStation) {
            return;
        }
        if (selectedWorkStation.active) {
            toggleActiveLabel.textContent = 'Dezactivează';
            toggleActiveButton.classList.remove('btn-success');
            toggleActiveButton.classList.add('btn-danger');
        } else {
            toggleActiveLabel.textContent = 'Activează';
            toggleActiveButton.classList.remove('btn-danger');
            toggleActiveButton.classList.add('btn-success');
        }
    }

    (function autoSelectFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const selectedId = params.get('selected');
        if (!selectedId) {
            return;
        }
        const item = list ? list.querySelector(
            '.workstation-item[data-id="' + selectedId.replace(/"/g, '') + '"]') : null;
        if (item) {
            const cleanUrl = window.location.pathname + window.location.hash;
            window.history.replaceState({}, document.title, cleanUrl);

            item.scrollIntoView({ block: 'nearest' });
            item.click();
        }
    })();

    if (toggleActiveButton) {
        toggleActiveButton.addEventListener('click', function () {
            if (!selectedWorkStation) {
                return;
            }

            const targetActive = !selectedWorkStation.active;
            toggleActiveButton.disabled = true;

            apiRequest({
                method: 'POST',
                path: APP_WORKSTATION_SET_ACTIVE_URL,
                body: { id: selectedWorkStation.id, active: targetActive },
                onSuccess: function () {
                    queueToast('success', targetActive ? 'Stație activată.' : 'Stație dezactivată.');
                    reloadKeepingSelection(selectedWorkStation.id);
                },
                onError: function (err) {
                    showToast('error', err && err.message ? err.message : 'Eroare la modificare.');
                    toggleActiveButton.disabled = false;
                }
            });
        });
    }

    const addSubmit = document.getElementById('addSubmitButton');
    const addSpinner = document.getElementById('addSubmitSpinner');

    if (addSubmit) {
        addSubmit.addEventListener('click', function () {
            const nameInput = document.getElementById('addName');
            const ipInput = document.getElementById('addIp');
            const ouSelect = document.getElementById('addOu');
            const activeInput = document.getElementById('addActive');

            const name = nameInput ? nameInput.value.trim() : '';
            const ip = ipInput ? ipInput.value.trim() : '';
            const ouValue = ouSelect ? ouSelect.value : '';
            const active = activeInput ? activeInput.checked : true;

            if (!name) {
                showToast('error', 'Numele stației este obligatoriu.');
                if (nameInput) { nameInput.focus(); }
                return;
            }
            if (!ouValue) {
                showToast('error', 'Selectați unitatea organizațională.');
                if (ouSelect) { ouSelect.focus(); }
                return;
            }

            addSubmit.disabled = true;
            if (addSpinner) {
                addSpinner.classList.remove('d-none');
            }

            const restore = function () {
                addSubmit.disabled = false;
                if (addSpinner) {
                    addSpinner.classList.add('d-none');
                }
            };

            apiRequest({
                method: 'POST',
                path: APP_WORKSTATION_CREATE_URL,
                body: {
                    name: name,
                    ip: ip === '' ? null : ip,
                    organizationalUnitId: parseInt(ouValue, 10),
                    active: active
                },
                onSuccess: function (data) {
                    queueToast('success', 'Stație de lucru creată.');
                    if (data && data.id) {
                        reloadKeepingSelection(data.id);
                    } else {
                        window.location.reload();
                    }
                },
                onError: function (err) {
                    showToast('error', err && err.message ? err.message : 'Eroare la creare.');
                    restore();
                }
            });
        });
    }
});
