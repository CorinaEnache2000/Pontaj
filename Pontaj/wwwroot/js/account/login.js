// Page script for Views/Account/Login.cshtml.
// JS-driven validation + submit (no <form>, no default browser handling).

if (consumeSessionExpiredFlag()) {
    document.getElementById('session-expired-alert').classList.remove('d-none');
}

const usernameInput = document.getElementById('username');
const passwordInput = document.getElementById('password');
const loginSubmitBtn = document.getElementById('login-submit');
const loginSpinner = document.getElementById('login-spinner');

usernameInput.focus();

function tryLogin() {
    const username = usernameInput.value.trim();
    const password = passwordInput.value;

    if (!username) {
        showToast('error', 'Vă rugăm să introduceți utilizatorul.');
        usernameInput.focus();
        return;
    }

    if (!password) {
        showToast('error', 'Vă rugăm să introduceți parola.');
        passwordInput.focus();
        return;
    }

    loginSubmitBtn.disabled = true;
    loginSpinner.classList.remove('d-none');

    apiRequest({
        method: 'POST',
        path: '/api/account/login',
        skipAuth: true,
        body: { username: username, password: password },
        onSuccess: function () {
            window.location.href = '/';
        },
        onError: function (err) {
            loginSubmitBtn.disabled = false;
            loginSpinner.classList.add('d-none');
            showToast('error', err.message || 'Autentificarea a eșuat.');
            passwordInput.value = '';
            passwordInput.focus();
        }
    });
}

loginSubmitBtn.addEventListener('click', tryLogin);

function handleEnter(e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        tryLogin();
    }
}

usernameInput.addEventListener('keydown', handleEnter);
passwordInput.addEventListener('keydown', handleEnter);
