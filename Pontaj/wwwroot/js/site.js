function normalizeForSearch(value) {
    if (value == null) {
        return '';
    }
    return value
        .toString()
        .normalize('NFD')
        .replace(/\p{M}/gu, '')
        .toLowerCase();
}

function attachSelectSearch(selectEl, placeholder) {
    if (!selectEl || selectEl.dataset.searchAttached === '1') {
        return;
    }
    selectEl.dataset.searchAttached = '1';

    const isMulti = selectEl.multiple;

    selectEl.style.display = 'none';

    const wrapper = document.createElement('div');
    wrapper.className = 'dropdown w-100 select-search';

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'form-select form-select-sm text-start select-search-toggle';
    button.setAttribute('data-bs-toggle', 'dropdown');
    button.setAttribute('aria-expanded', 'false');
    if (isMulti) {
        button.setAttribute('data-bs-auto-close', 'outside');
        button.style.height = 'auto';
        button.style.minHeight = 'calc(1.5em + 0.5rem + 2px)';
    }

    const emptyOptionLabel = (function () {
        const empty = selectEl.querySelector('option[value=""]');
        return empty ? empty.textContent : '';
    })();

    function refreshLabel() {
        if (isMulti) {
            const selected = Array.from(selectEl.selectedOptions)
                .filter(function (o) { return o.value !== ''; });
            button.textContent = '';
            if (selected.length === 0) {
                button.textContent = emptyOptionLabel;
            } else {
                selected.forEach(function (o) {
                    const row = document.createElement('div');
                    row.textContent = o.textContent;
                    button.appendChild(row);
                });
            }
        } else {
            const current = selectEl.selectedOptions[0];
            button.textContent = current ? current.textContent : '';
        }
    }
    refreshLabel();
    selectEl.addEventListener('change', refreshLabel);

    const menu = document.createElement('div');
    menu.className = 'dropdown-menu w-100 p-1 shadow-sm select-search-menu';

    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.className = 'form-control form-control-sm mb-1';
    searchInput.placeholder = placeholder || 'Caută...';
    searchInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
        }
    });
    menu.appendChild(searchInput);

    const itemsList = document.createElement('div');
    itemsList.className = 'select-search-items';
    itemsList.style.maxHeight = '16rem';
    itemsList.style.overflowY = 'auto';
    menu.appendChild(itemsList);

    Array.from(selectEl.options).forEach(function (opt) {
        if (opt.value === '' && (selectEl.required || isMulti)) {
            return;
        }

        const item = document.createElement('button');
        item.type = 'button';
        item.className = 'dropdown-item small select-search-item d-flex align-items-center py-1 px-2';

        let checkbox = null;
        if (isMulti) {
            checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'form-check-input me-1 mt-0 flex-shrink-0';
            checkbox.checked = opt.selected;
            checkbox.tabIndex = -1;
            checkbox.style.pointerEvents = 'none';
            item.appendChild(checkbox);
            item.appendChild(document.createTextNode(opt.textContent));
        } else {
            item.textContent = opt.textContent;
        }

        item.dataset.value = opt.value;
        item.dataset.normalized = normalizeForSearch(opt.textContent);
        if (!isMulti && opt.selected) {
            item.classList.add('active');
        }
        item.addEventListener('click', function () {
            if (isMulti) {
                opt.selected = !opt.selected;
                if (checkbox) { checkbox.checked = opt.selected; }
            } else {
                selectEl.value = opt.value;
                itemsList.querySelectorAll('.select-search-item.active').forEach(function (el) {
                    el.classList.remove('active');
                });
                item.classList.add('active');
            }
            refreshLabel();
            selectEl.dispatchEvent(new Event('change', { bubbles: true }));
        });
        itemsList.appendChild(item);
    });

    searchInput.addEventListener('input', function () {
        const term = normalizeForSearch(searchInput.value.trim());
        const items = itemsList.querySelectorAll('.select-search-item');
        items.forEach(function (it) {
            const norm = it.dataset.normalized || '';
            if (term === '' || norm.indexOf(term) !== -1) {
                it.classList.remove('d-none');
            } else {
                it.classList.add('d-none');
            }
        });
    });

    wrapper.appendChild(button);
    wrapper.appendChild(menu);
    selectEl.parentNode.insertBefore(wrapper, selectEl.nextSibling);

    wrapper.addEventListener('shown.bs.dropdown', function () {
        searchInput.value = '';
        itemsList.querySelectorAll('.select-search-item.d-none').forEach(function (el) {
            el.classList.remove('d-none');
        });
        itemsList.querySelectorAll('.select-search-item.active').forEach(function (el) {
            el.classList.remove('active');
        });
        const items = itemsList.querySelectorAll('.select-search-item');
        if (isMulti) {
            const selectedValues = new Set(
                Array.from(selectEl.selectedOptions).map(function (o) { return o.value; }));
            items.forEach(function (it) {
                const isSelected = selectedValues.has(it.dataset.value);
                const cb = it.querySelector('input[type="checkbox"]');
                if (cb) {
                    cb.checked = isSelected;
                }
            });
        } else {
            for (let i = 0; i < items.length; i++) {
                if (items[i].dataset.value === selectEl.value) {
                    items[i].classList.add('active');
                    break;
                }
            }
        }
        searchInput.focus();
    });
}

function reloadKeepingSelection(id) {
    const url = new URL(window.location.href);
    url.searchParams.set('selected', String(id));
    window.location.href = url.toString();
}

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

    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const months = ['ian.', 'feb.', 'mar.', 'apr.', 'mai', 'iun.', 'iul.', 'aug.', 'sep.', 'oct.', 'noi.', 'dec.'];
    const month = months[now.getMonth()];
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    momentEl.textContent = day + ' ' + month + ' ' + hours + ':' + minutes;

    const isHTML = /<\/?[a-z][\s\S]*>/i.test(content);

    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = content;
    const plainText = tempDiv.textContent || tempDiv.innerText || '';
    const wordCount = plainText.trim().split(/\s+/).filter(Boolean).length;

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
                path: '/api/account/logout'
            });

            clearSessionToken();
            localStorage.clear();
            sessionStorage.clear();
            window.location.href = '/Account/Login';
        });
    }
});
