// Organizational-units page: searchable parent/child tree on the left,
// tabbed details (General / Stații de lucru) on the right. The tree is
// rendered server-side (recursive _OrganizationalUnitNode partial); tab
// content is fetched as HTML partials via apiRequest.

const APP_OU_TAB_URLS = {
    OrganizationalUnitGeneralInfo: '/Admin/OrganizationalUnitGeneralInfo',
    OrganizationalUnitWorkStations: '/Admin/OrganizationalUnitWorkStations'
};

document.addEventListener('DOMContentLoaded', function () {
    const tree = document.getElementById('ouTree');
    if (!tree) {
        return;
    }

    const searchInput = document.getElementById('ouSearch');
    const noMatch = document.getElementById('ouNoMatch');

    const details = document.getElementById('ouDetails');
    const detailsContent = document.getElementById('ouDetailsContent');
    const selectedName = document.getElementById('selectedOuName');
    const placeholder = document.getElementById('ouWorkAreaPlaceholder');
    const tabs = document.getElementById('ouTabs');

    let currentOuId = null;
    let isTabLoading = false;

    function allNodes() {
        return tree.querySelectorAll('.ou-node');
    }

    function childNodes(node) {
        return Array.from(node.querySelectorAll(':scope > ul.ou-children > li.ou-node'));
    }

    // ---- Tree click: caret toggles, name selects -------------------------
    tree.addEventListener('click', function (event) {
        const toggle = event.target.closest('.ou-toggle');
        if (toggle) {
            const node = toggle.closest('.ou-node');
            if (node) {
                node.classList.toggle('collapsed');
            }
            return;
        }

        const nameEl = event.target.closest('.ou-name');
        if (nameEl) {
            const node = nameEl.closest('.ou-node');
            if (node) {
                selectNode(node, nameEl);
            }
        }
    });

    function selectNode(node, nameEl) {
        const id = node.getAttribute('data-id');
        if (!id) {
            return;
        }
        currentOuId = id;

        const selected = tree.querySelectorAll('.ou-node.ou-selected');
        selected.forEach(function (el) {
            el.classList.remove('ou-selected');
        });
        node.classList.add('ou-selected');

        // First text node holds the name (an "inactiv" badge may follow).
        const firstText = nameEl.childNodes[0];
        selectedName.textContent = firstText ? firstText.textContent.trim() : nameEl.textContent.trim();

        placeholder.classList.add('d-none');
        details.classList.remove('d-none');

        setActiveTab('OrganizationalUnitGeneralInfo');
        loadTab('OrganizationalUnitGeneralInfo');
    }

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
            if (!btn || isTabLoading || !currentOuId) {
                return;
            }
            const tabName = btn.getAttribute('data-tab');
            setActiveTab(tabName);
            loadTab(tabName);
        });
    }

    function loadTab(tabName) {
        if (!currentOuId) {
            return;
        }
        const url = APP_OU_TAB_URLS[tabName];
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
            query: { id: currentOuId },
            expect: 'html',
            onSuccess: function (html) {
                detailsContent.innerHTML = html;
                release();
            },
            onError: function (err) {
                detailsContent.innerHTML =
                    '<p class="text-danger mt-2">Eroare la încărcarea detaliilor unității.</p>';
                if (typeof showToast === 'function') {
                    showToast('error', err && err.message ? err.message : 'Eroare la încărcare.');
                }
                release();
            }
        });
    }

    // ---- Name search -----------------------------------------------------
    // Returns true if this node or any descendant matches the term.
    function filterNode(node, term) {
        const name = normalizeForSearch(node.getAttribute('data-name') || '');
        let descendantMatch = false;
        childNodes(node).forEach(function (child) {
            if (filterNode(child, term)) {
                descendantMatch = true;
            }
        });

        const selfMatch = name.indexOf(term) !== -1;
        const visible = selfMatch || descendantMatch;

        if (visible) {
            node.classList.remove('d-none');
        } else {
            node.classList.add('d-none');
        }

        if (selfMatch) {
            node.classList.add('ou-match');
        } else {
            node.classList.remove('ou-match');
        }

        if (descendantMatch) {
            node.classList.remove('collapsed');
        }

        return visible;
    }

    function applyFilter() {
        const term = normalizeForSearch(searchInput.value.trim());

        if (term === '') {
            // Cleared search: show everything again but return to the
            // closed-by-default state.
            allNodes().forEach(function (node) {
                node.classList.remove('d-none');
                node.classList.remove('ou-match');
                node.classList.add('collapsed');
            });
            if (noMatch) {
                noMatch.classList.add('d-none');
            }
            return;
        }

        const roots = Array.from(tree.querySelectorAll('ul.ou-root > li.ou-node'));
        let anyVisible = false;
        roots.forEach(function (root) {
            if (filterNode(root, term)) {
                anyVisible = true;
            }
        });

        if (noMatch) {
            if (anyVisible) {
                noMatch.classList.add('d-none');
            } else {
                noMatch.classList.remove('d-none');
            }
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', applyFilter);
    }
});
