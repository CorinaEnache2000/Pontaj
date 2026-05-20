const APP_TOKEN_KEY = 'sessionToken';
const APP_SESSION_EXPIRED_FLAG = 'isSessionExpired';
const APP_LOGIN_URL = '/Account/Login';
const APP_REQUEST_TIMEOUT_SECONDS = 30;
const APP_COOKIE_ATTRS = '; Path=/; SameSite=Strict; Secure';

function apiRequest({
    method,
    path,
    body = null,
    bodyType = 'json',
    query = null,
    expect = 'json',
    skipAuth = false,
    timeoutSeconds = APP_REQUEST_TIMEOUT_SECONDS,
    onSuccess = null,
    onError = null
}) {
    let token = null;
    if (!skipAuth) {
        token = localStorage.getItem(APP_TOKEN_KEY);
        if (token && isJwtExpired(token)) {
            handleSessionExpired();
            return null;
        }
    }

    let url = path;
    if (query) {
        const qs = typeof query === 'string' ? query : encodeUrlForm(query);
        if (qs.length > 0) {
            url += (url.includes('?') ? '&' : '?') + qs;
        }
    }

    const xhr = new XMLHttpRequest();
    xhr.open(method, url, true);
    xhr.timeout = timeoutSeconds * 1000;

    let bearerSent = false;
    if (token) {
        xhr.setRequestHeader('Authorization', 'Bearer ' + token);
        bearerSent = true;
    }

    let payload = null;
    if (body != null) {
        if (body instanceof FormData) {
            payload = body;
        } else if (bodyType === 'form') {
            xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=utf-8');
            payload = encodeUrlForm(body);
        } else {
            xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
            payload = JSON.stringify(body);
        }
    }

    if (expect === 'blob') {
        xhr.responseType = 'blob';
    }

    xhr.onload = function () {
        if (xhr.status === 401 && bearerSent) {
            handleSessionExpired();
            return;
        }

        const ok = xhr.status >= 200 && xhr.status < 300;

        if (expect === 'html') {
            if (ok) {
                onSuccess?.(xhr.responseText);
            } else {
                dispatchError({ status: xhr.status, message: xhr.statusText || 'Cererea a eșuat.', data: xhr.responseText }, onError);
            }
            return;
        }

        if (expect === 'blob') {
            if (ok) {
                onSuccess?.(xhr.response);
            } else {
                dispatchError({ status: xhr.status, message: xhr.statusText || 'Cererea a eșuat.' }, onError);
            }
            return;
        }

        let envelope = null;
        try {
            envelope = xhr.responseText ? JSON.parse(xhr.responseText) : null;
        } catch (e) {
            dispatchError({ status: xhr.status, message: 'Răspuns JSON invalid de la server.', data: xhr.responseText }, onError);
            return;
        }

        if (envelope && typeof envelope.token === 'string' && envelope.token.length > 0) {
            writeSessionToken(envelope.token);
        }

        const isSuccess = envelope?.status === 'success' || (ok && envelope?.status !== 'error');
        if (isSuccess) {
            onSuccess?.(envelope?.data ?? null);
        } else {
            dispatchError({
                status: xhr.status,
                message: envelope?.reason || xhr.statusText || 'Cererea a eșuat.',
                reason: envelope?.reason ?? null,
                data: envelope?.data ?? null
            }, onError);
        }
    };

    xhr.ontimeout = function () {
        dispatchError({ status: 0, message: 'Cererea a depășit timpul de așteptare.' }, onError);
    };

    xhr.onerror = function () {
        dispatchError({ status: 0, message: 'Eroare de rețea.' }, onError);
    };

    xhr.onabort = function () {
        dispatchError({ status: 0, message: 'Cererea a fost anulată.' }, onError);
    };

    try {
        xhr.send(payload);
    } catch (err) {
        dispatchError({ status: 0, message: 'Eroare la trimiterea cererii.' }, onError);
        return null;
    }
    return xhr;
}

function encodeUrlForm(obj) {
    const parts = [];
    for (const [k, v] of Object.entries(obj)) {
        if (v == null) {
            continue;
        }
        parts.push(encodeURIComponent(k) + '=' + encodeURIComponent(v));
    }
    return parts.join('&');
}

function isJwtExpired(token) {
    if (typeof token !== 'string') {
        return true;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
        return true;
    }

    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');

    let payloadJson;
    try {
        payloadJson = atob(base64);
    } catch (err) {
        return true;
    }

    let payload;
    try {
        payload = JSON.parse(payloadJson);
    } catch (err) {
        return true;
    }

    if (!payload.exp) {
        return true;
    }

    const currentTimeInSeconds = Math.floor(Date.now() / 1000);
    return payload.exp < currentTimeInSeconds;
}

function dispatchError(err, onError) {
    if (typeof onError === 'function') {
        onError(err);
    } else if (typeof showToast === 'function') {
        showToast('error', err.message || 'Cererea a eșuat.');
    } else {
        console.error('apiRequest error (no onError, no showToast):', err);
    }
}

function handleSessionExpired() {
    clearSessionToken();
    sessionStorage.setItem(APP_SESSION_EXPIRED_FLAG, '1');
    if (window.location.pathname.toLowerCase() !== APP_LOGIN_URL.toLowerCase()) {
        window.location.href = APP_LOGIN_URL;
    }
}

function writeSessionToken(token) {
    localStorage.setItem(APP_TOKEN_KEY, token);
    document.cookie = APP_TOKEN_KEY + '=' + encodeURIComponent(token) + APP_COOKIE_ATTRS;
}

function clearSessionToken() {
    localStorage.removeItem(APP_TOKEN_KEY);
    document.cookie = APP_TOKEN_KEY + '=; Expires=Thu, 01 Jan 1970 00:00:00 GMT' + APP_COOKIE_ATTRS;
}

function setAuthToken(token) {
    if (token) {
        writeSessionToken(token);
    } else {
        clearSessionToken();
    }
}

function getAuthToken() {
    return localStorage.getItem(APP_TOKEN_KEY);
}

function clearAuthToken() {
    clearSessionToken();
}

function consumeSessionExpiredFlag() {
    const flag = sessionStorage.getItem(APP_SESSION_EXPIRED_FLAG);
    if (flag) {
        sessionStorage.removeItem(APP_SESSION_EXPIRED_FLAG);
    }
    return flag === '1';
}
