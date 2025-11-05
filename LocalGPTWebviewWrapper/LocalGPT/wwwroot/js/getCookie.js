window.getCookie = function (name) {
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        const [k, v] = cookie.trim().split('=');
        if (k === name) return decodeURIComponent(v);
    }
    return null;
}