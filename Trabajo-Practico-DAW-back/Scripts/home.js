const homePeliculasContainer = document.getElementById("homePeliculasContainer");
const btnActualizarHomePeliculas = document.getElementById("btnActualizarHomePeliculas");

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
    const poster = pelicula.posterUrl && pelicula.posterUrl.trim() !== ""
        ? pelicula.posterUrl
        : "../Multimedia/cinetopia-logo.png";

    homePeliculasContainer.innerHTML += `
        <article class="home-movie-card">
            <img src="${poster}" alt="Portada de ${pelicula.nombre}">

            <div class="home-movie-card-content">
                <h3>${pelicula.nombre}</h3>
                <p><strong>Descripción:</strong> ${pelicula.descripcion}</p>
                <p><strong>Género:</strong> ${pelicula.genero || "Sin género"}</p>
                <p><strong>Director:</strong> ${pelicula.director || "Sin director"}</p>
                <p><strong>Año:</strong> ${pelicula.anio || "Sin año"}</p>
            </div>
        </article>
    `;
        });

    } catch (error) {
        homePeliculasContainer.innerHTML = `
            <p>No se pudieron cargar las películas.</p>
            <p>${error.message}</p>
        `;
    }
}