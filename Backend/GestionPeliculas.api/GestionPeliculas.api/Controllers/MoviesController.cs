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

            if (string.IsNullOrWhiteSpace(movie.Nombre))
            {
                return BadRequest("El nombre de la película es obligatorio");
            }

            if (string.IsNullOrWhiteSpace(movie.Descripcion))
            {
                return BadRequest("La descripción de la película es obligatoria");
            }

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

            if (string.IsNullOrWhiteSpace(movie.Nombre))
            {
                return BadRequest("El nombre de la película es obligatorio");
            }

            if (string.IsNullOrWhiteSpace(movie.Descripcion))
            {
                return BadRequest("La descripción de la película es obligatoria");
            }

            existingMovie.Nombre = movie.Nombre;
            existingMovie.Descripcion = movie.Descripcion;
            existingMovie.Genero = movie.Genero;
            existingMovie.Director = movie.Director;
            existingMovie.PosterUrl = movie.PosterUrl;
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
    }
}