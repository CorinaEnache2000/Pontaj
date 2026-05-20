const loginParams = new URLSearchParams(window.location.search);
const expiredFromQuery = loginParams.get('expired') === '1';

if (consumeSessionExpiredFlag() || expiredFromQuery) {
    document.getElementById('session-expired-alert').classList.remove('d-none');
}

if (expiredFromQuery) {
    loginParams.delete('expired');
    const newSearch = loginParams.toString();
    const newUrl = window.location.pathname + (newSearch ? '?' + newSearch : '') + window.location.hash;
    window.history.replaceState({}, document.title, newUrl);
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

const togglePasswordBtn = document.getElementById('toggle-password');
const togglePasswordIcon = document.getElementById('toggle-password-icon');

togglePasswordBtn.addEventListener('click', function () {
    const willShow = passwordInput.type === 'password';
    passwordInput.type = willShow ? 'text' : 'password';

    togglePasswordIcon.classList.toggle('bi-eye', !willShow);
    togglePasswordIcon.classList.toggle('bi-eye-slash', willShow);

    const label = willShow ? 'Ascunde parola' : 'Afișează parola';
    togglePasswordBtn.setAttribute('aria-label', label);
    togglePasswordBtn.setAttribute('title', label);

    passwordInput.focus();
});
