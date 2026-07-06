const API_URL = "https://localhost:7243/api";

function getToken() {
    return localStorage.getItem("token");
}

async function apiRequest(endpoint, method = "GET", body = null, requiresAuth = true) {
    const headers = {
        "Content-Type": "application/json"
    };

    if (requiresAuth) {
        const token = getToken();

        if (!token) {
            throw new Error("No hay token. Primero tenés que iniciar sesión.");
        }

        headers["Authorization"] = `Bearer ${token}`;
    }

    const options = {
        method: method,
        headers: headers
    };

    if (body !== null) {
        options.body = JSON.stringify(body);
    }

    const response = await fetch(`${API_URL}${endpoint}`, options);

    if (response.status === 401) {
        throw new Error("401 - No estás logueado o el token es inválido.");
    }

    if (response.status === 403) {
        throw new Error("403 - No tenés privilegios para esta operación.");
    }

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Error en la petición.");
    }

    const contentType = response.headers.get("content-type");

    if (contentType && contentType.includes("application/json")) {
        return await response.json();
    }

    return await response.text();
}

/* =========================
   AUTH
========================= */

async function login(userName, password) {
    return await apiRequest("/Auth/login", "POST", {
        userName,
        password
    }, false);
}

async function register(userName, email, password) {
    return await apiRequest("/Auth/register", "POST", {
        userName,
        email,
        password
    }, false);
}

async function refreshToken(refreshToken) {
    return await apiRequest("/Auth/refresh", "POST", {
        refreshToken
    }, false);
}

/* =========================
   MOVIES
========================= */

async function getMovies() {
    return await apiRequest("/Movies", "GET");
}

async function getMovieById(id) {
    return await apiRequest(`/Movies/${id}`, "GET");
}

async function createMovie(movie) {
    return await apiRequest("/Movies", "POST", movie);
}

async function updateMovie(id, movie) {
    return await apiRequest(`/Movies/${id}`, "PUT", movie);
}

async function deleteMovie(id) {
    return await apiRequest(`/Movies/${id}`, "DELETE");
}

/* =========================
   USERS
========================= */

async function getUsers() {
    return await apiRequest("/Users", "GET");
}

async function getUserById(id) {
    return await apiRequest(`/Users/${id}`, "GET");
}

async function createUser(user) {
    return await apiRequest("/Users", "POST", user);
}

async function updateUser(id, user) {
    return await apiRequest(`/Users/${id}`, "PUT", user);
}

async function deleteUser(id) {
    return await apiRequest(`/Users/${id}`, "DELETE");
}

async function getPrivileges() {
    return await apiRequest("/Users/privileges", "GET");
}

async function assignPrivileges(id, privileges) {
    return await apiRequest(`/Users/${id}/privileges`, "POST", {
        privileges
    });
}