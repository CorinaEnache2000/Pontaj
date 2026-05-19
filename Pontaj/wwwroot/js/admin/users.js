// Users page: searchable list on the left, tabbed details (General / Roluri)
// on the right. List is rendered server-side (_UsersList); tab content is
// fetched as HTML partials via apiRequest, same pattern as the OU page.

const APP_USER_TAB_URLS = {
    UserGeneralInfo: '/Admin/UserGeneralInfo',
    UserRoles: '/Admin/UserRoles'
};

document.addEventListener('DOMContentLoaded', function () {
    const list = document.getElementById('userList');
    if (!list) {
        return;
    }

    const searchInput = document.getElementById('userSearch');
    const details = document.getElementById('userDetails');
    const detailsContent = document.getElementById('userDetailsContent');
    const selectedName = document.getElementById('selectedUserName');
    const placeholder = document.getElementById('userWorkAreaPlaceholder');
    const tabs = document.getElementById('userTabs');

    let currentUserId = null;
    let isTabLoading = false;

    // ---- Client-side filter (diacritic-insensitive) ----------------------
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const filter = normalizeForSearch(searchInput.value.trim());
            const items = list.querySelectorAll('.user-item');
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

    // ---- Row click → select user ----------------------------------------
    list.addEventListener('click', function (event) {
        const item = event.target.closest('.user-item');
        if (!item || isTabLoading) {
            return;
        }

        const id = item.getAttribute('data-id');
        if (!id) {
            return;
        }
        currentUserId = id;

        const active = list.querySelectorAll('.user-item.active');
        active.forEach(function (el) {
            el.classList.remove('active');
        });
        item.classList.add('active');

        const nameEl = item.querySelector('b');
        selectedName.textContent = nameEl ? nameEl.textContent : '';

        placeholder.classList.add('d-none');
        details.classList.remove('d-none');

        setActiveTab('UserGeneralInfo');
        loadTab('UserGeneralInfo');
    });

    // ---- Tabs ------------------------------------------------------------
    function setActiveTab(tabName) {
        const buttons = tabs.querySelectorAll('button[data-tab]');
        buttons.forEach(function (btn) {
            if (btn.getAttribute('data-tab') === tabName) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    }

    if (tabs) {
        tabs.addEventListener('click', function (event) {
            const btn = event.target.closest('button[data-tab]');
            if (!btn || isTabLoading || !currentUserId) {
                return;
            }
            const tabName = btn.getAttribute('data-tab');
            setActiveTab(tabName);
            loadTab(tabName);
        });
    }

    function loadTab(tabName) {
        if (!currentUserId) {
            return;
        }
        const url = APP_USER_TAB_URLS[tabName];
        if (!url) {
            return;
        }

        isTabLoading = true;
        document.body.style.cursor = 'wait';
        detailsContent.innerHTML = '<div class="text-muted mt-2">Se încarcă...</div>';

        const release = function () {
            isTabLoading = false;
            document.body.style.cursor = 'default';
        };

        apiRequest({
            method: 'GET',
            path: url,
            query: { id: currentUserId },
            expect: 'html',
            onSuccess: function (html) {
                detailsContent.innerHTML = html;
                release();
            },
            onError: function (err) {
                detailsContent.innerHTML =
                    '<p class="text-danger mt-2">Eroare la încărcarea detaliilor utilizatorului.</p>';
                if (typeof showToast === 'function') {
                    showToast('error', err && err.message ? err.message : 'Eroare la încărcare.');
                }
                release();
            }
        });
    }
});
