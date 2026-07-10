const usuarioLogueado = document.getElementById("usuarioLogueado");
const rolUsuario = document.getElementById("rolUsuario");
const tablaUsuarios = document.getElementById("tablaUsuarios");

let privilegiosDisponibles = [];

document.addEventListener("DOMContentLoaded", async function () {
    protegerPaginaAdmin();
    mostrarDatosUsuario();

    await cargarPrivilegios();
    await cargarUsuarios();
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
        alert("No tenés permisos para administrar usuarios.");
        window.location.href = "Pages/Home.html";
    }
}

function mostrarDatosUsuario() {
    const userName = localStorage.getItem("userName") || "Sin usuario";
    const privileges = JSON.parse(localStorage.getItem("privileges") || "[]");

    usuarioLogueado.innerText = `Usuario: ${userName}`;

    if (privileges.includes("USERS_MANAGE")) {
        rolUsuario.innerText = "Rol: Administrador";
    } else {
        rolUsuario.innerText = "Rol: Usuario";
    }
}

document.getElementById("btnVolver").addEventListener("click", function () {
    window.location.href = "Pages/Home.html";
});

document.getElementById("btnListarUsuarios").addEventListener("click", async function () {
    await cargarUsuarios();
});

async function cargarPrivilegios() {
    try {
        privilegiosDisponibles = await getPrivileges();

        renderizarCheckboxes("privilegiosCrear", []);
        renderizarCheckboxes("privilegiosAsignar", []);

    } catch (error) {
        alert(error.message);
    }
}

function renderizarCheckboxes(contenedorId, privilegiosSeleccionados) {
    const contenedor = document.getElementById(contenedorId);
    contenedor.innerHTML = "";

    privilegiosDisponibles.forEach(privilegio => {
        const checked = privilegiosSeleccionados.includes(privilegio.description) ? "checked" : "";

        contenedor.innerHTML += `
            <label style="display:block; margin-bottom: 5px;">
                <input type="checkbox" value="${privilegio.description}" ${checked}>
                ${traducirPrivilegio(privilegio.description)}
            </label>
        `;
    });
}

function obtenerPrivilegiosSeleccionados(contenedorId) {
    const checkboxes = document.querySelectorAll(`#${contenedorId} input[type="checkbox"]:checked`);

    return Array.from(checkboxes).map(checkbox => checkbox.value);
}

function traducirPrivilegio(privilegio) {
    const traducciones = {
        "MOVIES_VIEW": "Ver películas",
        "MOVIES_CREATE": "Crear películas",
        "MOVIES_EDIT": "Editar películas",
        "MOVIES_DELETE": "Borrar películas",
        "USERS_MANAGE": "Administrar usuarios"
    };

    return traducciones[privilegio] || privilegio;
}

async function cargarUsuarios() {
    try {
        const usuarios = await getUsers();

        tablaUsuarios.innerHTML = "";

        if (usuarios.length === 0) {
            tablaUsuarios.innerHTML = `
                <tr>
                    <td colspan="6">No hay usuarios cargados.</td>
                </tr>
            `;
            return;
        }

        usuarios.forEach(usuario => {
            const privilegiosTraducidos = usuario.privileges
                .map(p => traducirPrivilegio(p))
                .join(", ");

            tablaUsuarios.innerHTML += `
                <tr>
                    <td>${usuario.id}</td>
                    <td>${usuario.userName}</td>
                    <td>${usuario.email}</td>
                    <td>${usuario.isActive ? "Sí" : "No"}</td>
                    <td>${privilegiosTraducidos}</td>
                    <td>
                        <button onclick="cargarDatosUsuario(${usuario.id})">Editar</button>
                        <button onclick="cargarPrivilegiosUsuario(${usuario.id})">Privilegios</button>
                        <button onclick="bajaUsuario(${usuario.id})">Dar de baja</button>
                    </td>
                </tr>
            `;
        });

    } catch (error) {
        alert(error.message);
    }
}

document.getElementById("formCrearUsuario").addEventListener("submit", async function (event) {
    event.preventDefault();

    const privilegios = obtenerPrivilegiosSeleccionados("privilegiosCrear");

    const usuario = {
        userName: document.getElementById("crearUserName").value,
        email: document.getElementById("crearEmail").value,
        password: document.getElementById("crearPassword").value,
        privileges: privilegios
    };

    try {
        await createUser(usuario);

        alert("Usuario creado correctamente.");

        this.reset();
        renderizarCheckboxes("privilegiosCrear", []);

        await cargarUsuarios();

    } catch (error) {
        alert(error.message);
    }
});

async function cargarDatosUsuario(id) {
    try {
        const usuario = await getUserById(id);

        document.getElementById("editarUserId").value = usuario.id;
        document.getElementById("editarUserName").value = usuario.userName;
        document.getElementById("editarEmail").value = usuario.email;
        document.getElementById("editarActivo").value = usuario.isActive.toString();

        document.getElementById("seccionEditarUsuario").hidden = false;

        window.scrollTo({
            top: document.body.scrollHeight,
            behavior: "smooth"
        });

    } catch (error) {
        alert(error.message);
    }
}

document.getElementById("formEditarUsuario").addEventListener("submit", async function (event) {
    event.preventDefault();

    const id = document.getElementById("editarUserId").value;

    const usuario = {
        userName: document.getElementById("editarUserName").value,
        email: document.getElementById("editarEmail").value,
        isActive: document.getElementById("editarActivo").value === "true"
    };

    try {
        await updateUser(id, usuario);

        alert("Usuario actualizado correctamente.");

        this.reset();
        document.getElementById("seccionEditarUsuario").hidden = true;

        await cargarUsuarios();

    } catch (error) {
        alert(error.message);
    }
});

async function cargarPrivilegiosUsuario(id) {
    try {
        const usuario = await getUserById(id);

        document.getElementById("privilegiosUserId").value = usuario.id;
        document.getElementById("usuarioPrivilegiosSeleccionado").innerText = `Usuario seleccionado: ${usuario.userName}`;

        renderizarCheckboxes("privilegiosAsignar", usuario.privileges);

        document.getElementById("seccionPrivilegiosUsuario").hidden = false;

        window.scrollTo({
            top: document.body.scrollHeight,
            behavior: "smooth"
        });

    } catch (error) {
        alert(error.message);
    }
}

document.getElementById("formAsignarPrivilegios").addEventListener("submit", async function (event) {
    event.preventDefault();

    const id = document.getElementById("privilegiosUserId").value;
    const privilegios = obtenerPrivilegiosSeleccionados("privilegiosAsignar");

    try {
        await assignPrivileges(id, privilegios);

        alert("Privilegios actualizados correctamente.");

        document.getElementById("seccionPrivilegiosUsuario").hidden = true;

        await cargarUsuarios();

    } catch (error) {
        alert(error.message);
    }
});

async function bajaUsuario(id) {
    const confirmar = confirm("¿Seguro que querés dar de baja este usuario?");

    if (!confirmar) {
        return;
    }

    try {
        await deleteUser(id);

        alert("Usuario dado de baja correctamente.");

        await cargarUsuarios();

    } catch (error) {
        alert(error.message);
    }
}