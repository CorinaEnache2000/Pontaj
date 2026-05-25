document.addEventListener('DOMContentLoaded', function () {
    if (!window.APP_SCAN_IS_LINKED) {
        return;
    }

    const employeesSelect = document.getElementById('filterEmployees');
    const workStationsSelect = document.getElementById('filterWorkStations');
    const ousSelect = document.getElementById('filterOus');
    const fromInput = document.getElementById('filterFrom');
    const toInput = document.getElementById('filterTo');
    const applyBtn = document.getElementById('filterApply');
    const resetBtn = document.getElementById('filterReset');

    const tableBody = document.getElementById('scansTableBody');
    const pageLabel = document.getElementById('scansPageLabel');
    const totalLabel = document.getElementById('scansTotalLabel');
    const pageSizeSelect = document.getElementById('scansPageSize');
    const prevBtn = document.getElementById('scansPrev');
    const nextBtn = document.getElementById('scansNext');

    const activeFiltersContainer = document.getElementById('scansActiveFilters');
    const activeFiltersBadge = document.getElementById('scansActiveFiltersBadge');
    const clearFiltersBtn = document.getElementById('scansClearFilters');

    const ouTreeRoot = document.getElementById('scanOuTree');
    const ouTreeSearch = document.getElementById('scanOuTreeSearch');
    const ouTreeNoMatch = document.getElementById('scanOuTreeNoMatch');
    let treeApplyTimeout = null;

    if (employeesSelect && typeof attachSelectSearch === 'function') {
        attachSelectSearch(employeesSelect, 'Introduceți numele');
    }
    if (workStationsSelect && typeof attachSelectSearch === 'function') {
        attachSelectSearch(workStationsSelect, 'Introduceți numele');
    }

    const formEmployee = document.getElementById('formEmployee');
    const formWorkStation = document.getElementById('formWorkStation');
    const formOu = document.getElementById('formOu');
    if (formEmployee && typeof attachSelectSearch === 'function') {
        attachSelectSearch(formEmployee, 'Introduceți numele');
    }
    if (formWorkStation && typeof attachSelectSearch === 'function') {
        attachSelectSearch(formWorkStation, 'Introduceți numele');
    }
    if (formOu && typeof attachSelectSearch === 'function') {
        attachSelectSearch(formOu, 'Introduceți numele');
    }

    const selectedOuIds = new Set();
    const ouNameById = new Map();
    if (ousSelect) {
        Array.from(ousSelect.options).forEach(function (o) {
            if (o.value !== '') {
                ouNameById.set(parseInt(o.value, 10), o.textContent);
            }
        });
    }

    setupOuTree();

    let state = {
        page: 1,
        pageSize: 50,
        totalPages: 1,
        activeFilters: readFilters()
    };

    function readFilters() {
        return {
            employeeIds: selectedIds(employeesSelect),
            workStationIds: selectedIds(workStationsSelect),
            organizationalUnitIds: Array.from(selectedOuIds),
            from: fromInput && fromInput.value ? fromInput.value : null,
            to: toInput && toInput.value ? toInput.value : null
        };
    }

    function selectedIds(selectEl) {
        if (!selectEl) {
            return [];
        }
        return Array.from(selectEl.selectedOptions)
            .map(function (o) { return o.value; })
            .filter(function (v) { return v !== ''; })
            .map(function (v) { return parseInt(v, 10); })
            .filter(function (v) { return !isNaN(v); });
    }

    function setupOuTree() {
        if (!ouTreeRoot) {
            return;
        }

        ouTreeRoot.addEventListener('click', function (e) {
            const toggle = e.target.closest('.ou-toggle');
            if (toggle) {
                const node = toggle.closest('.ou-node');
                if (node) {
                    node.classList.toggle('collapsed');
                }
                return;
            }
            const checkbox = e.target.closest('.ou-checkbox');
            if (checkbox) {
                const node = checkbox.closest('.ou-node');
                if (!node) { return; }
                const checked = checkbox.checked;
                applyCheck(node, checked);
                refreshAncestors(node.parentElement);
                scheduleTreeApply();
                return;
            }
            const nameEl = e.target.closest('.ou-name');
            if (nameEl) {
                const node = nameEl.closest('.ou-node');
                if (!node) { return; }
                const cb = node.querySelector(':scope > .ou-row > .ou-checkbox');
                if (!cb) { return; }
                cb.checked = !cb.checked;
                applyCheck(node, cb.checked);
                refreshAncestors(node.parentElement);
                scheduleTreeApply();
            }
        });

        if (ouTreeSearch) {
            ouTreeSearch.addEventListener('input', function () {
                filterTree(ouTreeSearch.value);
            });
        }
    }

    function scheduleTreeApply() {
        if (treeApplyTimeout) {
            clearTimeout(treeApplyTimeout);
        }
        treeApplyTimeout = setTimeout(function () {
            treeApplyTimeout = null;
            state.activeFilters = readFilters();
            renderActiveFilters();
            loadPage(1);
        }, 300);
    }

    function applyCheck(node, checked) {
        node.querySelectorAll('.ou-checkbox').forEach(function (cb) {
            cb.checked = checked;
            cb.indeterminate = false;
            const id = parseInt(cb.getAttribute('data-id'), 10);
            if (!isNaN(id)) {
                if (checked) {
                    selectedOuIds.add(id);
                } else {
                    selectedOuIds.delete(id);
                }
            }
        });
    }

    function refreshAncestors(parentUl) {
        while (parentUl && parentUl.classList && parentUl.classList.contains('ou-children')) {
            const parentNode = parentUl.parentElement;
            if (!parentNode || !parentNode.classList || !parentNode.classList.contains('ou-node')) {
                break;
            }
            const parentCheckbox = parentNode.querySelector(':scope > .ou-row > .ou-checkbox');
            if (!parentCheckbox) {
                break;
            }
            const descendantBoxes = parentUl.querySelectorAll('.ou-checkbox');
            let allChecked = descendantBoxes.length > 0;
            let anyChecked = false;
            descendantBoxes.forEach(function (cb) {
                if (cb.checked) {
                    anyChecked = true;
                } else {
                    allChecked = false;
                }
                if (cb.indeterminate) {
                    anyChecked = true;
                    allChecked = false;
                }
            });
            parentCheckbox.checked = allChecked;
            parentCheckbox.indeterminate = !allChecked && anyChecked;
            const parentId = parseInt(parentCheckbox.getAttribute('data-id'), 10);
            if (!isNaN(parentId)) {
                if (allChecked) {
                    selectedOuIds.add(parentId);
                } else {
                    selectedOuIds.delete(parentId);
                }
            }
            parentUl = parentNode.parentElement;
        }
    }

    function filterTree(rawTerm) {
        if (!ouTreeRoot) {
            return;
        }
        const term = (typeof normalizeForSearch === 'function')
            ? normalizeForSearch((rawTerm || '').trim())
            : (rawTerm || '').trim().toLowerCase();

        const nodes = ouTreeRoot.querySelectorAll('.ou-node');
        nodes.forEach(function (n) {
            n.classList.remove('d-none', 'ou-match');
        });

        if (term === '') {
            if (ouTreeNoMatch) { ouTreeNoMatch.classList.add('d-none'); }
            return;
        }

        let anyMatch = false;
        nodes.forEach(function (n) {
            const name = (typeof normalizeForSearch === 'function')
                ? normalizeForSearch(n.getAttribute('data-name') || '')
                : (n.getAttribute('data-name') || '').toLowerCase();
            if (name.indexOf(term) !== -1) {
                n.classList.add('ou-match');
                anyMatch = true;
                let parent = n.parentElement;
                while (parent && parent !== ouTreeRoot) {
                    if (parent.classList && parent.classList.contains('ou-node')) {
                        parent.classList.remove('collapsed');
                    }
                    parent = parent.parentElement;
                }
                n.querySelectorAll('.ou-node').forEach(function (d) {
                    d.classList.remove('d-none');
                });
            }
        });

        nodes.forEach(function (n) {
            if (!n.classList.contains('ou-match')) {
                const hasMatchInside = n.querySelector('.ou-node.ou-match');
                const hasMatchAncestor = (function () {
                    let p = n.parentElement;
                    while (p && p !== ouTreeRoot) {
                        if (p.classList && p.classList.contains('ou-node') && p.classList.contains('ou-match')) {
                            return true;
                        }
                        p = p.parentElement;
                    }
                    return false;
                })();
                if (!hasMatchInside && !hasMatchAncestor) {
                    n.classList.add('d-none');
                }
            }
        });

        if (ouTreeNoMatch) {
            if (anyMatch) {
                ouTreeNoMatch.classList.add('d-none');
            } else {
                ouTreeNoMatch.classList.remove('d-none');
            }
        }
    }

    function loadPage(page) {
        state.page = page;
        const body = Object.assign({
            page: state.page,
            pageSize: state.pageSize
        }, {
            employeeIds: state.activeFilters.employeeIds,
            workStationIds: state.activeFilters.workStationIds,
            organizationalUnitIds: state.activeFilters.organizationalUnitIds,
            from: state.activeFilters.from,
            to: state.activeFilters.to
        });

        tableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted fst-italic">Se încarcă...</td></tr>';

        apiRequest({
            method: 'POST',
            path: window.APP_SCAN_LIST_URL,
            body: body,
            onSuccess: function (data) {
                if (!data) {
                    renderRows([]);
                    return;
                }
                const items = data.items || [];
                const total = data.total || 0;
                const newPageSize = data.pageSize || state.pageSize;
                const totalPages = Math.max(1, Math.ceil(total / newPageSize));
                if (items.length === 0 && state.page > 1 && total > 0) {
                    loadPage(Math.min(state.page - 1, totalPages));
                    return;
                }
                renderRows(items);
                state.pageSize = newPageSize;
                state.totalPages = totalPages;
                if (state.page > state.totalPages) {
                    state.page = state.totalPages;
                }
                pageLabel.textContent = state.page + ' / ' + state.totalPages;
                totalLabel.textContent = String(total);
                prevBtn.disabled = state.page <= 1;
                nextBtn.disabled = state.page >= state.totalPages;
            },
            onError: function (err) {
                tableBody.innerHTML = '<tr><td colspan="9" class="text-center text-danger">' +
                    escapeHtml(err && err.message ? err.message : 'Eroare la încărcare.') + '</td></tr>';
            }
        });
    }

    function renderRows(items) {
        if (!items.length) {
            tableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted fst-italic">Nu există pontaje pentru filtrele selectate.</td></tr>';
            return;
        }
        const canEdit = !!window.APP_SCAN_CAN_EDIT;
        const showIp = !!window.APP_SCAN_SHOW_IP;
        const html = items.map(function (it) {
            const pill = it.inOut
                ? '<span class="direction-pill direction-in">Intrare</span>'
                : '<span class="direction-pill direction-out">Ieșire</span>';
            const ipCell = showIp ? '<td>' + escapeHtml(it.ip || '—') + '</td>' : '';
            const actions = canEdit
                ? '<td class="text-nowrap">'
                    + '<button type="button" class="btn btn-link btn-sm p-0 me-2 scan-edit" data-id="' + it.id + '" title="Editează"><i class="bi bi-pencil"></i></button>'
                    + '<button type="button" class="btn btn-link btn-sm p-0 text-danger scan-delete" data-id="' + it.id + '" title="Șterge"><i class="bi bi-trash"></i></button>'
                    + '</td>'
                : '';
            return '<tr data-id="' + it.id + '"'
                + ' data-employee-id="' + it.employeeId + '"'
                + ' data-employee-name="' + escapeAttr(it.employeeName || '') + '"'
                + ' data-badge="' + escapeAttr(it.badge || '') + '"'
                + ' data-inout="' + (it.inOut ? '1' : '0') + '"'
                + ' data-moment="' + escapeAttr(it.moment || '') + '"'
                + ' data-workstation-id="' + (it.workStationId == null ? '' : it.workStationId) + '"'
                + ' data-workstation="' + escapeAttr(it.workStation || '') + '"'
                + ' data-ou-id="' + (it.organizationalUnitId == null ? '' : it.organizationalUnitId) + '"'
                + ' data-ou="' + escapeAttr(it.organizationalUnit || '') + '">'
                + '<td>' + escapeHtml(it.employeeName || '') + '</td>'
                + '<td>' + escapeHtml(it.mark || '—') + '</td>'
                + '<td>' + escapeHtml(it.badge || '—') + '</td>'
                + '<td>' + pill + '</td>'
                + '<td class="text-nowrap">' + formatMoment(it.moment) + '</td>'
                + '<td>' + escapeHtml(it.workStation || '—') + '</td>'
                + '<td>' + escapeHtml(it.organizationalUnit || '—') + '</td>'
                + ipCell
                + actions
                + '</tr>';
        }).join('');
        tableBody.innerHTML = html;
    }

    function renderActiveFilters() {
        if (!activeFiltersContainer || !activeFiltersBadge) {
            return;
        }
        activeFiltersBadge.textContent = '';
        const f = state.activeFilters;
        const chips = [];

        function pushChipsFromSelect(label, selectEl, ids) {
            if (!selectEl || !ids || ids.length === 0) {
                return;
            }
            const byId = new Map();
            Array.from(selectEl.options).forEach(function (o) {
                if (o.value !== '') {
                    byId.set(parseInt(o.value, 10), o.textContent);
                }
            });
            ids.forEach(function (id) {
                const txt = byId.get(id) || String(id);
                chips.push(label + ': ' + txt);
            });
        }

        function pushChipsFromMap(label, map, ids) {
            if (!ids || ids.length === 0) {
                return;
            }
            ids.forEach(function (id) {
                const txt = map.get(id) || String(id);
                chips.push(label + ': ' + txt);
            });
        }

        pushChipsFromSelect('Angajat', employeesSelect, f.employeeIds);
        pushChipsFromSelect('Stație', workStationsSelect, f.workStationIds);
        pushChipsFromMap('Unitate', ouNameById, f.organizationalUnitIds);
        if (f.from) {
            chips.push('De la: ' + f.from);
        }
        if (f.to) {
            chips.push('Până la: ' + f.to);
        }

        chips.forEach(function (text) {
            const span = document.createElement('span');
            span.className = 'badge bg-info text-dark';
            span.textContent = text;
            activeFiltersBadge.appendChild(span);
        });

        if (chips.length === 0) {
            activeFiltersContainer.classList.add('d-none');
        } else {
            activeFiltersContainer.classList.remove('d-none');
        }
    }

    if (applyBtn) {
        applyBtn.addEventListener('click', function () {
            state.activeFilters = readFilters();
            renderActiveFilters();
            loadPage(1);
        });
    }

    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            resetSelect(employeesSelect);
            resetSelect(workStationsSelect);
            if (fromInput) { fromInput.value = ''; }
            if (toInput) { toInput.value = ''; }
        });
    }

    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function () {
            resetSelect(employeesSelect);
            resetSelect(workStationsSelect);
            selectedOuIds.clear();
            if (ouTreeRoot) {
                ouTreeRoot.querySelectorAll('.ou-checkbox').forEach(function (cb) {
                    cb.checked = false;
                    cb.indeterminate = false;
                });
            }
            if (fromInput) { fromInput.value = ''; }
            if (toInput) { toInput.value = ''; }
            state.activeFilters = readFilters();
            renderActiveFilters();
            loadPage(1);
        });
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener('change', function () {
            state.pageSize = parseInt(pageSizeSelect.value, 10) || 50;
            loadPage(1);
        });
    }
    if (prevBtn) {
        prevBtn.addEventListener('click', function () {
            if (state.page > 1) { loadPage(state.page - 1); }
        });
    }
    if (nextBtn) {
        nextBtn.addEventListener('click', function () {
            if (state.page < state.totalPages) { loadPage(state.page + 1); }
        });
    }

    if (window.APP_SCAN_CAN_EDIT) {
        wireEditDelete();
    }

    loadPage(1);

    function resetSelect(selectEl) {
        if (!selectEl) { return; }
        Array.from(selectEl.options).forEach(function (o) { o.selected = false; });
        selectEl.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function wireEditDelete() {
        const modalEl = document.getElementById('scanFormModal');
        const modalTitle = document.getElementById('scanFormModalLabel');
        const scanIdInput = document.getElementById('formScanId');
        const formEmployeeNote = document.getElementById('formEmployeeReadonlyNote');
        const formMomentInput = document.getElementById('formMoment');
        const formSubmitBtn = document.getElementById('formSubmit');
        const formSpinner = document.getElementById('formSubmitSpinner');

        const deleteModalEl = document.getElementById('scanDeleteModal');
        const deleteSummary = document.getElementById('scanDeleteSummary');
        const deleteConfirmBtn = document.getElementById('deleteConfirmButton');
        const deleteSpinner = document.getElementById('deleteConfirmSpinner');
        let pendingDeleteId = null;

        document.getElementById('scanAddButton').addEventListener('click', function () {
            modalTitle.textContent = 'Adaugă pontaj';
            scanIdInput.value = '';
            setSelectValue(formEmployee, '');
            setSelectValue(formWorkStation, '');
            setSelectValue(formOu, '');
            formEmployee.disabled = false;
            if (formEmployeeNote) { formEmployeeNote.style.display = 'none'; }
            document.getElementById('formInOutIn').checked = true;
            formMomentInput.value = formatDateTimeForInput(new Date());
        });

        tableBody.addEventListener('click', function (e) {
            const editBtn = e.target.closest('.scan-edit');
            const delBtn = e.target.closest('.scan-delete');
            if (editBtn) {
                const row = editBtn.closest('tr');
                openEditModal(row);
            } else if (delBtn) {
                const row = delBtn.closest('tr');
                openDeleteModal(row);
            }
        });

        function openEditModal(row) {
            modalTitle.textContent = 'Editează pontaj';
            scanIdInput.value = row.dataset.id;
            setSelectValue(formEmployee, row.dataset.employeeId);
            formEmployee.disabled = true;
            if (formEmployeeNote) { formEmployeeNote.style.display = 'block'; }
            document.getElementById('formInOutIn').checked = row.dataset.inout === '1';
            document.getElementById('formInOutOut').checked = row.dataset.inout !== '1';
            formMomentInput.value = momentToInput(row.dataset.moment);
            setSelectValue(formWorkStation, row.dataset.workstationId || '');
            setSelectValue(formOu, row.dataset.ouId || '');
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        }

        function openDeleteModal(row) {
            pendingDeleteId = row.dataset.id;
            const direction = row.dataset.inout === '1' ? 'Intrare' : 'Ieșire';
            deleteSummary.innerHTML = '<strong>' + escapeHtml(row.dataset.employeeName) + '</strong>'
                + ' &middot; ' + direction
                + ' &middot; ' + formatMoment(row.dataset.moment);
            bootstrap.Modal.getOrCreateInstance(deleteModalEl).show();
        }

        formSubmitBtn.addEventListener('click', function () {
            const moment = formMomentInput.value;
            if (!moment) {
                showToast('error', 'Selectați momentul.');
                return;
            }
            const inOut = document.getElementById('formInOutIn').checked;
            const wsValue = formWorkStation.value;
            const ouValue = formOu.value;
            const id = scanIdInput.value;

            if (ouValue !== '' && ouNameById.size > 0) {
                const ouNum = parseInt(ouValue, 10);
                if (isNaN(ouNum) || !ouNameById.has(ouNum)) {
                    showToast('error', 'Unitatea organizațională selectată nu este în aria dumneavoastră.');
                    return;
                }
            }

            formSpinner.classList.remove('d-none');
            formSubmitBtn.disabled = true;

            function restore() {
                formSpinner.classList.add('d-none');
                formSubmitBtn.disabled = false;
            }

            if (id) {
                apiRequest({
                    method: 'POST',
                    path: window.APP_SCAN_UPDATE_URL,
                    body: {
                        id: parseInt(id, 10),
                        inOut: inOut,
                        moment: moment,
                        workStationId: wsValue ? parseInt(wsValue, 10) : null,
                        organizationalUnitId: ouValue ? parseInt(ouValue, 10) : null
                    },
                    onSuccess: function () {
                        restore();
                        bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                        showToast('success', 'Pontaj actualizat.');
                        loadPage(state.page);
                    },
                    onError: function (err) {
                        restore();
                        showToast('error', err && err.message ? err.message : 'Eroare la salvare.');
                    }
                });
            } else {
                const empValue = formEmployee.value;
                if (!empValue) {
                    showToast('error', 'Selectați un angajat.');
                    restore();
                    return;
                }
                apiRequest({
                    method: 'POST',
                    path: window.APP_SCAN_CREATE_URL,
                    body: {
                        employeeId: parseInt(empValue, 10),
                        inOut: inOut,
                        moment: moment,
                        workStationId: wsValue ? parseInt(wsValue, 10) : null,
                        organizationalUnitId: ouValue ? parseInt(ouValue, 10) : null
                    },
                    onSuccess: function () {
                        restore();
                        bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                        showToast('success', 'Pontaj adăugat.');
                        loadPage(1);
                    },
                    onError: function (err) {
                        restore();
                        showToast('error', err && err.message ? err.message : 'Eroare la salvare.');
                    }
                });
            }
        });

        deleteConfirmBtn.addEventListener('click', function () {
            if (!pendingDeleteId) {
                return;
            }
            deleteSpinner.classList.remove('d-none');
            deleteConfirmBtn.disabled = true;
            apiRequest({
                method: 'POST',
                path: window.APP_SCAN_DELETE_URL,
                body: { id: parseInt(pendingDeleteId, 10) },
                onSuccess: function () {
                    deleteSpinner.classList.add('d-none');
                    deleteConfirmBtn.disabled = false;
                    pendingDeleteId = null;
                    bootstrap.Modal.getOrCreateInstance(deleteModalEl).hide();
                    showToast('success', 'Pontaj șters.');
                    loadPage(state.page);
                },
                onError: function (err) {
                    deleteSpinner.classList.add('d-none');
                    deleteConfirmBtn.disabled = false;
                    showToast('error', err && err.message ? err.message : 'Eroare la ștergere.');
                }
            });
        });
    }

    function setSelectValue(selectEl, value) {
        if (!selectEl) { return; }
        selectEl.value = value;
        selectEl.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function formatDateTimeForInput(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        const hh = String(d.getHours()).padStart(2, '0');
        const mm = String(d.getMinutes()).padStart(2, '0');
        const ss = String(d.getSeconds()).padStart(2, '0');
        return y + '-' + m + '-' + day + 'T' + hh + ':' + mm + ':' + ss;
    }

    function momentToInput(iso) {
        if (!iso) { return ''; }
        const parts = iso.split('T');
        if (parts.length !== 2) { return iso; }
        const timeParts = parts[1].split(':');
        const hh = timeParts[0] || '00';
        const mm = timeParts[1] || '00';
        const ss = (timeParts[2] || '00').substring(0, 2);
        return parts[0] + 'T' + hh + ':' + mm + ':' + ss;
    }

    function formatMoment(iso) {
        if (!iso) { return ''; }
        const parts = iso.split('T');
        if (parts.length !== 2) { return escapeHtml(iso); }
        const dateParts = parts[0].split('-');
        if (dateParts.length !== 3) { return escapeHtml(iso); }
        const timeParts = parts[1].split(':');
        const hh = timeParts[0] || '00';
        const mm = timeParts[1] || '00';
        const ss = (timeParts[2] || '00').substring(0, 2);
        return dateParts[2] + '.' + dateParts[1] + '.' + dateParts[0] + ' ' + hh + ':' + mm + ':' + ss;
    }

    function escapeHtml(s) {
        if (s == null) { return ''; }
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function escapeAttr(s) {
        if (s == null) { return ''; }
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }
});
