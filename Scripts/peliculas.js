const usuarioLogueado = document.getElementById("usuarioLogueado");
const rolUsuario = document.getElementById("rolUsuario");
const tablaPeliculas = document.getElementById("tablaPeliculas");

document.addEventListener("DOMContentLoaded", async function () {
    protegerPagina();
    mostrarDatosUsuario();
    await cargarPeliculas();
});

function protegerPagina() {
    const token = localStorage.getItem("token");

    if (!token) {
        alert("Primero tenés que iniciar sesión.");
        window.location.href = "LogIn.html";
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

document.getElementById("btnListarPeliculas").addEventListener("click", async function () {
    await cargarPeliculas();
});

async function cargarPeliculas() {
    try {
        const peliculas = await getMovies();

        tablaPeliculas.innerHTML = "";

        if (peliculas.length === 0) {
            tablaPeliculas.innerHTML = `
                <tr>
                    <td colspan="8">No hay películas cargadas.</td>
                </tr>
            `;
            return;
        }

        peliculas.forEach(pelicula => {
                    const poster = pelicula.posterUrl && pelicula.posterUrl.trim() !== ""
                ? pelicula.posterUrl
                : "Multimedia/cinetopia-logo.png";

            tablaPeliculas.innerHTML += `
                <tr>
                    <td>${pelicula.id}</td>
                    <td>
                        <img 
                            src="${poster}" 
                            alt="Portada de ${pelicula.nombre}" 
                            class="poster-mini"
                            onerror="this.src='Multimedia/cinetopia-logo.png'"
                        >
                    </td>
                    <td>${pelicula.nombre}</td>
                    <td>${pelicula.descripcion}</td>
                    <td>${pelicula.genero || ""}</td>
                    <td>${pelicula.director || ""}</td>
                    <td>${pelicula.anio || ""}</td>
                    <td>
                        <button onclick="cargarDatosParaEditar(${pelicula.id})">Editar</button>
                        <button onclick="borrarPelicula(${pelicula.id})">Borrar</button>
                    </td>
                </tr>
            `;
        });

    } catch (error) {
        alert(error.message);
    }
}

document.getElementById("formCrearPelicula").addEventListener("submit", async function (event) {
    event.preventDefault();

    const pelicula = {
        nombre: document.getElementById("nombre").value,
        descripcion: document.getElementById("descripcion").value,
        genero: document.getElementById("genero").value,
        director: document.getElementById("director").value,
        anio: obtenerAnio("anio"),
        posterUrl: document.getElementById("posterUrl").value,
        isActive: true
    };

    try {
        await createMovie(pelicula);

        alert("Película creada correctamente.");

        this.reset();

        await cargarPeliculas();

    } catch (error) {
        alert(error.message);
    }
});

async function cargarDatosParaEditar(id) {
    try {
        const pelicula = await getMovieById(id);

        document.getElementById("editarId").value = pelicula.id;
        document.getElementById("editarNombre").value = pelicula.nombre;
        document.getElementById("editarDescripcion").value = pelicula.descripcion;
        document.getElementById("editarGenero").value = pelicula.genero || "";
        document.getElementById("editarDirector").value = pelicula.director || "";
        document.getElementById("editarAnio").value = pelicula.anio || "";
        document.getElementById("editarPosterUrl").value = pelicula.posterUrl || "";
        document.getElementById("seccionEditar").hidden = false;

        window.scrollTo({
            top: document.body.scrollHeight,
            behavior: "smooth"
        });

    } catch (error) {
        alert(error.message);
    }
}

document.getElementById("formEditarPelicula").addEventListener("submit", async function (event) {
    event.preventDefault();

    const id = document.getElementById("editarId").value;

    if (!id) {
        alert("Primero seleccioná una película para editar.");
        return;
    }

    const pelicula = {
        nombre: document.getElementById("editarNombre").value,
        descripcion: document.getElementById("editarDescripcion").value,
        genero: document.getElementById("editarGenero").value,
        director: document.getElementById("editarDirector").value,
        anio: obtenerAnio("editarAnio"),
        posterUrl: document.getElementById("editarPosterUrl").value,
        isActive: true
    };

    try {
        await updateMovie(id, pelicula);

        alert("Película editada correctamente.");

        this.reset();

        await cargarPeliculas();

    } catch (error) {
        alert(error.message);
    }
});

async function borrarPelicula(id) {
    const confirmar = confirm("¿Seguro que querés borrar esta película?");

    if (!confirmar) {
        return;
    }

    try {
        await deleteMovie(id);

        alert("Película dada de baja correctamente.");

        await cargarPeliculas();

    } catch (error) {
        alert(error.message);
    }
}

function obtenerAnio(inputId) {
    const valor = document.getElementById(inputId).value;

    if (valor === "") {
        return null;
    }

    return parseInt(valor);
}