const tablaBitacora = document.getElementById("tablaBitacora");

document.addEventListener("DOMContentLoaded", async function () {
    protegerPaginaAdmin();
    await cargarBitacora();
});

function protegerPaginaAdmin() {
    const token = localStorage.getItem("token");
    const privileges = JSON.parse(localStorage.getItem("privileges") || "[]");

    if (!token) {
        alert("Primero tenés que iniciar sesión.");
        window.location.href = "LogIn.html";
        return;
    }

    if (!privileges.includes("USERS_MANAGE")) {
        alert("No tenés permisos para ver la bitácora.");
        window.location.href = "Pages/Home.html";
    }
}

document.getElementById("btnVolver").addEventListener("click", function () {
    window.location.href = "Pages/Home.html";
});

document.getElementById("btnActualizarBitacora").addEventListener("click", async function () {
    await cargarBitacora();
});

async function cargarBitacora() {
    try {
        const logs = await getAuditLogs();

        tablaBitacora.innerHTML = "";

        if (logs.length === 0) {
            tablaBitacora.innerHTML = `
                <tr>
                    <td colspan="3">No hay acciones registradas.</td>
                </tr>
            `;
            return;
        }

        logs.forEach(log => {
            const fecha = new Date(log.fechaHora).toLocaleString();

            tablaBitacora.innerHTML += `
                <tr>
                    <td>${fecha}</td>
                    <td>${log.usuario}</td>
                    <td>${log.accion}</td>
                </tr>
            `;
        });

    } catch (error) {
        alert(error.message);
    }
}