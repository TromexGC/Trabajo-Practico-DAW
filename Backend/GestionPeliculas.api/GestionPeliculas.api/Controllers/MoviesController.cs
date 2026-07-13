using GestionPeliculas.api.Data;
using GestionPeliculas.api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestionPeliculas.api.Services;

namespace GestionPeliculas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MoviesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;
        public MoviesController(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }
        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario desconocido";
        }

        private bool HasPrivilege(string privilege)
        {
            return User.Claims.Any(c => c.Type == "privilege" && c.Value == privilege);
        }

        [HttpGet]
        public async Task<IActionResult> GetMovies()
        {
            if (!HasPrivilege("MOVIES_VIEW"))
            {
                return StatusCode(403, "No tiene privilegios para visualizar películas");
            }

            var movies = await _context.Movies
                .Where(m => m.IsActive)
                .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            if (!HasPrivilege("MOVIES_VIEW"))
            {
                return StatusCode(403, "No tiene privilegios para visualizar películas");
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (movie == null)
            {
                return NotFound("Película no encontrada");
            }

            return Ok(movie);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie(Movie movie)
        {
            if (!HasPrivilege("MOVIES_CREATE"))
            {
                return StatusCode(403, "No tiene privilegios para crear películas");
            }

            var errorValidacion = ValidarPelicula(movie);

            if (errorValidacion != null)
            {
                return BadRequest(errorValidacion);
            }

            movie.Nombre = movie.Nombre.Trim();
            movie.Descripcion = movie.Descripcion.Trim();
            movie.Genero = movie.Genero?.Trim();
            movie.Director = movie.Director?.Trim();
            movie.PosterUrl = movie.PosterUrl?.Trim();
            movie.IsActive = true;

            _context.Movies.Add(movie);

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Creó la película: {movie.Nombre}"
            );

            return Ok(movie);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovie(int id, Movie movie)
        {
            if (!HasPrivilege("MOVIES_EDIT"))
            {
                return StatusCode(403, "No tiene privilegios para editar películas");
            }

            var existingMovie = await _context.Movies.FindAsync(id);

            if (existingMovie == null || !existingMovie.IsActive)
            {
                return NotFound("Película no encontrada");
            }

            var errorValidacion = ValidarPelicula(movie);

            if (errorValidacion != null)
            {
                return BadRequest(errorValidacion);
            }

            existingMovie.Nombre = movie.Nombre.Trim();
            existingMovie.Descripcion = movie.Descripcion.Trim();
            existingMovie.Genero = movie.Genero?.Trim();
            existingMovie.Director = movie.Director?.Trim();
            existingMovie.PosterUrl = movie.PosterUrl?.Trim();
            existingMovie.Anio = movie.Anio;

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Editó la película: {existingMovie.Nombre}"
            );

            return Ok(existingMovie);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            if (!HasPrivilege("MOVIES_DELETE"))
            {
                return StatusCode(403, "No tiene privilegios para borrar películas");
            }

            var movie = await _context.Movies.FindAsync(id);

            if (movie == null || !movie.IsActive)
            {
                return NotFound("Película no encontrada");
            }

            movie.IsActive = false;
            await _context.SaveChangesAsync();
            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Dio de baja la película: {movie.Nombre}"
            );

            return Ok("Película dada de baja correctamente");
        }
        private string? ValidarPelicula(Movie movie)
        {
            if (string.IsNullOrWhiteSpace(movie.Nombre))
            {
                return "El nombre de la película es obligatorio.";
            }

            if (movie.Nombre.Length > 100)
            {
                return "El nombre de la película no puede superar los 100 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(movie.Descripcion))
            {
                return "La descripción de la película es obligatoria.";
            }

            if (movie.Descripcion.Length > 500)
            {
                return "La descripción de la película no puede superar los 500 caracteres.";
            }

            if (!string.IsNullOrWhiteSpace(movie.Genero) && movie.Genero.Length > 50)
            {
                return "El género no puede superar los 50 caracteres.";
            }

            if (!string.IsNullOrWhiteSpace(movie.Director) && movie.Director.Length > 100)
            {
                return "El director no puede superar los 100 caracteres.";
            }

            if (movie.Anio.HasValue)
            {
                int anioActual = DateTime.Now.Year;

                if (movie.Anio < 1900 || movie.Anio > anioActual)
                {
                    return $"El año debe estar entre 1900 y {anioActual}.";
                }
            }

            if (!string.IsNullOrWhiteSpace(movie.PosterUrl))
            {
                if (movie.PosterUrl.Length > 500)
                {
                    return "La URL del póster no puede superar los 500 caracteres.";
                }

                bool esUrlValida = Uri.TryCreate(movie.PosterUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                if (!esUrlValida)
                {
                    return "La URL del póster no es válida. Debe comenzar con http:// o https://.";
                }
            }

            return null;
        }
    }

}