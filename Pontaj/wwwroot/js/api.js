// =============================================================================
// Pontaj backend client — XHR-based, JWT Bearer auth, sliding token refresh.
//
// Server contract (ResponseBase envelope, content-type application/json):
//   { status: 'success' | 'error',
//     reason: string | null,    // human-readable; null on success, set on error
//     data:   <any payload>,
//     token:  string | null }   // if non-null, client mirrors it to localStorage and a cookie
//
// The session token is mirrored to a 'sessionToken' cookie (SameSite=Strict, Secure, Path=/)
// so the browser auto-sends it on HTML navigations. Cookie is JS-set (not HttpOnly) and
// is cleared whenever the token is cleared or detected as expired client-side.
//
// HTML endpoints return content-type text/html and raw HTML. Use expect:'html'.
// =============================================================================

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
    // ---- Pre-flight: bail out early on a known-expired token ---------------
    let token = null;
    if (!skipAuth) {
        token = localStorage.getItem(APP_TOKEN_KEY);
        if (token && isJwtExpired(token)) {
            handleSessionExpired();
            return null;
        }
    }

    // ---- Build URL ---------------------------------------------------------
    let url = path;
    if (query) {
        const qs = typeof query === 'string' ? query : encodeUrlForm(query);
        if (qs.length > 0) {
            url += (url.includes('?') ? '&' : '?') + qs;
        }
    }

    // ---- Open + headers ----------------------------------------------------
    const xhr = new XMLHttpRequest();
    xhr.open(method, url, true);
    xhr.timeout = timeoutSeconds * 1000;

    let bearerSent = false;
    if (token) {
        xhr.setRequestHeader('Authorization', 'Bearer ' + token);
        bearerSent = true;
    }

    // ---- Build payload + Content-Type --------------------------------------
    let payload = null;
    if (body != null) {
        if (body instanceof FormData) {
            // multipart/form-data — browser sets the boundary; do NOT set Content-Type ourselves
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

    // ---- Response handling -------------------------------------------------
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

        // expect === 'json' → parse ResponseBase envelope
        let envelope = null;
        try {
            envelope = xhr.responseText ? JSON.parse(xhr.responseText) : null;
        } catch (e) {
            dispatchError({ status: xhr.status, message: 'Răspuns JSON invalid de la server.', data: xhr.responseText }, onError);
            return;
        }

        // Sliding-token refresh — mirror to localStorage and cookie
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

    xhr.send(payload);
    return xhr;
}

// =============================================================================
// Helpers
// =============================================================================

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

    // JWT uses base64url; convert to plain base64 for atob
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
