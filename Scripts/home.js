const homePeliculasContainer = document.getElementById("homePeliculasContainer");
const btnActualizarHomePeliculas = document.getElementById("btnActualizarHomePeliculas");

const movieModalOverlay = document.getElementById("movieModalOverlay");
const movieModalClose = document.getElementById("movieModalClose");
const movieModalPoster = document.getElementById("movieModalPoster");
const movieModalTitulo = document.getElementById("movieModalTitulo");
const movieModalDescripcion = document.getElementById("movieModalDescripcion");
const movieModalGenero = document.getElementById("movieModalGenero");
const movieModalDirector = document.getElementById("movieModalDirector");
const movieModalAnio = document.getElementById("movieModalAnio");

const POSTER_DEFAULT = "../Multimedia/cinetopia-logo.png";

document.addEventListener("DOMContentLoaded", async function () {
    protegerHome();
    await cargarPeliculasEnHome();
});

function protegerHome() {
    const token = localStorage.getItem("token");

    if (!token) {
        alert("Primero tenés que iniciar sesión.");
        window.location.href = "../LogIn.html";
    }
}

btnActualizarHomePeliculas.addEventListener("click", async function () {
    await cargarPeliculasEnHome();
});

async function cargarPeliculasEnHome() {
    try {
        const peliculas = await getMovies();

        homePeliculasContainer.innerHTML = "";

        if (peliculas.length === 0) {
            homePeliculasContainer.innerHTML = "<p>No hay películas cargadas todavía.</p>";
            return;
        }

        peliculas.forEach(pelicula => {
            homePeliculasContainer.appendChild(crearCardPelicula(pelicula));
        });

    } catch (error) {
        homePeliculasContainer.innerHTML = "";

        const mensajeError = document.createElement("p");
        mensajeError.textContent = "No se pudieron cargar las películas.";

        const detalleError = document.createElement("p");
        detalleError.textContent = error.message;

        homePeliculasContainer.append(mensajeError, detalleError);
    }
}

function crearCardPelicula(pelicula) {
    const poster = pelicula.posterUrl && pelicula.posterUrl.trim() !== ""
        ? pelicula.posterUrl
        : POSTER_DEFAULT;

    const card = document.createElement("article");
    card.className = "home-movie-card";
    card.tabIndex = 0;
    card.setAttribute("role", "button");
    card.setAttribute("aria-label", `Ver detalles de ${pelicula.nombre}`);

    const img = document.createElement("img");
    img.src = poster;
    img.alt = `Portada de ${pelicula.nombre}`;

    const overlay = document.createElement("div");
    overlay.className = "home-movie-card-overlay";

    const titulo = document.createElement("h3");
    titulo.textContent = pelicula.nombre;

    overlay.appendChild(titulo);
    card.append(img, overlay);

    card.addEventListener("click", () => abrirModalPelicula(pelicula, poster));
    card.addEventListener("keydown", (event) => {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            abrirModalPelicula(pelicula, poster);
        }
    });

    return card;
}

function abrirModalPelicula(pelicula, poster) {
    movieModalPoster.src = poster;
    movieModalPoster.alt = `Portada de ${pelicula.nombre}`;
    movieModalTitulo.textContent = pelicula.nombre;
    movieModalDescripcion.textContent = pelicula.descripcion;
    movieModalGenero.textContent = pelicula.genero || "Sin género";
    movieModalDirector.textContent = pelicula.director || "Sin director";
    movieModalAnio.textContent = pelicula.anio || "Sin año";

    movieModalOverlay.hidden = false;
}

function cerrarModalPelicula() {
    movieModalOverlay.hidden = true;
}

movieModalClose.addEventListener("click", cerrarModalPelicula);

movieModalOverlay.addEventListener("click", function (event) {
    if (event.target === movieModalOverlay) {
        cerrarModalPelicula();
    }
});

document.addEventListener("keydown", function (event) {
    if (event.key === "Escape" && !movieModalOverlay.hidden) {
        cerrarModalPelicula();
    }
});