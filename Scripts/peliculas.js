const usuario = document.getElementById("usuario");
const resultado = document.getElementById("resultado");

usuario.innerText = localStorage.getItem("userName") || "No logueado";

document.getElementById("btnLogout").addEventListener("click", function () {
    localStorage.clear();
    window.location.href = "LogIn.html";
});

document.getElementById("btnListar").addEventListener("click", async function () {
    try {
        const movies = await getMovies();

        resultado.innerHTML = "";

        if (movies.length === 0) {
            resultado.innerHTML = "<p>No hay películas cargadas.</p>";
            return;
        }

        movies.forEach(movie => {
            resultado.innerHTML += `
                <div style="border: 1px solid black; margin: 10px; padding: 10px;">
                    <p><strong>Id:</strong> ${movie.id}</p>
                    <p><strong>Nombre:</strong> ${movie.nombre}</p>
                    <p><strong>Descripción:</strong> ${movie.descripcion}</p>
                    <p><strong>Género:</strong> ${movie.genero || ""}</p>
                    <p><strong>Director:</strong> ${movie.director || ""}</p>
                    <p><strong>Año:</strong> ${movie.anio || ""}</p>
                </div>
            `;
        });

    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("formCrearPelicula").addEventListener("submit", async function (event) {
    event.preventDefault();

    const movie = {
        nombre: document.getElementById("nombre").value,
        descripcion: document.getElementById("descripcion").value,
        genero: document.getElementById("genero").value,
        director: document.getElementById("director").value,
        anio: parseInt(document.getElementById("anio").value),
        isActive: true
    };

    try {
        const nuevaPelicula = await createMovie(movie);
        alert("Película creada correctamente. Id: " + nuevaPelicula.id);
    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("formEditarPelicula").addEventListener("submit", async function (event) {
    event.preventDefault();

    const id = document.getElementById("editarId").value;

    const movie = {
        nombre: document.getElementById("editarNombre").value,
        descripcion: document.getElementById("editarDescripcion").value,
        genero: document.getElementById("editarGenero").value,
        director: document.getElementById("editarDirector").value,
        anio: parseInt(document.getElementById("editarAnio").value),
        isActive: true
    };

    try {
        await updateMovie(id, movie);
        alert("Película editada correctamente.");
    } catch (error) {
        alert(error.message);
    }
});

document.getElementById("formBorrarPelicula").addEventListener("submit", async function (event) {
    event.preventDefault();

    const id = document.getElementById("borrarId").value;

    try {
        await deleteMovie(id);
        alert("Película dada de baja correctamente.");
    } catch (error) {
        alert(error.message);
    }
});