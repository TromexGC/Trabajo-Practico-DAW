const API_URL = "https://localhost:7243/api";

const loginForm = document.getElementById("login-form");

loginForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const userName = document.getElementById("login-username").value;
    const password = document.getElementById("login-password").value;

    try {
        const response = await fetch(`${API_URL}/Auth/login`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                userName: userName,
                password: password
            })
        });

        if (!response.ok) {
            alert("Usuario o contraseña incorrectos");
            return;
        }

        const data = await response.json();

        localStorage.setItem("token", data.token);
        localStorage.setItem("refreshToken", data.refreshToken);
        localStorage.setItem("userName", data.userName);
        localStorage.setItem("privileges", JSON.stringify(data.privileges));

        alert("Login correcto");    
        window.location.href = "Pages/Home.html";

        console.log("Token:", data.token);
        console.log("Privilegios:", data.privileges);

    } catch (error) {
        console.error("Error en login:", error);
        alert("No se pudo conectar con la API");
    }
});

const registerForm = document.getElementById("register-form");

registerForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const email = document.getElementById("register-email").value;
    const userName = document.getElementById("register-username").value;
    const password = document.getElementById("register-password").value;
    const confirmPassword = document.getElementById("register-confirm-password").value;

    if (password !== confirmPassword) {
        alert("Las contraseñas no coinciden");
        return;
    }

    try {
        const response = await fetch(`${API_URL}/Auth/register`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                userName: userName,
                email: email,
                password: password
            })
        });

        if (!response.ok) {
            const error = await response.text();
            alert("Error al registrar usuario: " + error);
            return;
        }

        const data = await response.json();

        localStorage.setItem("token", data.token);
        localStorage.setItem("refreshToken", data.refreshToken);
        localStorage.setItem("userName", data.userName);
        localStorage.setItem("privileges", JSON.stringify(data.privileges));

        alert("Usuario registrado correctamente");

        console.log("Token:", data.token);
        console.log("Privilegios:", data.privileges);

    } catch (error) {
        console.error("Error en registro:", error);
        alert("No se pudo conectar con la API");
    }
});
async function resetPassword(email, newPassword, confirmPassword) {
    const response = await fetch(`${API_URL}/Auth/reset-password`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            email: email,
            newPassword: newPassword,
            confirmPassword: confirmPassword
        })
    });

    if (!response.ok) {
        const error = await response.text();
        throw new Error(error);
    }

    return await response.json();
}
const forgotPasswordForm = document.getElementById("forgot-password-form");

if (forgotPasswordForm) {
    forgotPasswordForm.addEventListener("submit", async function (event) {
        event.preventDefault();

        const email = document.getElementById("forgot-email").value;
        const newPassword = document.getElementById("forgot-new-password").value;
        const confirmPassword = document.getElementById("forgot-confirm-password").value;

        try {
            await resetPassword(email, newPassword, confirmPassword);

            alert("Contraseña actualizada correctamente. Ya podés iniciar sesión.");

            forgotPasswordForm.reset();

        } catch (error) {
            alert(error.message);
        }
    });
}