using GestionPeliculas.api.Data;
using GestionPeliculas.api.DTOs;
using GestionPeliculas.api.Models;
using GestionPeliculas.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionPeliculas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly AuditService _auditService;

        public UsersController(
            AppDbContext context,
            PasswordService passwordService,
            AuditService auditService)
        {
            _context = context;
            _passwordService = passwordService;
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
        public async Task<IActionResult> GetUsers()
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.IsActive,
                    Privileges = u.UserPrivileges
                        .Select(up => up.Privilege.Description)
                        .ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.IsActive,
                    Privileges = u.UserPrivileges
                        .Select(up => up.Privilege.Description)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            string userName = request.UserName?.Trim() ?? "";
            string email = request.Email?.Trim() ?? "";
            string password = request.Password ?? "";

            var errorValidacion = ValidarDatosUsuario(userName, email, password);

            if (errorValidacion != null)
            {
                return BadRequest(errorValidacion);
            }

            bool userExists = await _context.Users
                .AnyAsync(u => u.UserName == userName || u.Email == email);

            if (userExists)
            {
                return BadRequest("Ya existe un usuario con ese nombre o email");
            }

            var requestedPrivileges = request.Privileges == null
                ? new List<string>()
                : request.Privileges.Distinct().ToList();

            if (!requestedPrivileges.Any())
            {
                requestedPrivileges.Add("MOVIES_VIEW");
            }

            var privileges = await _context.Privileges
                .Where(p => requestedPrivileges.Contains(p.Description))
                .ToListAsync();

            var invalidPrivileges = requestedPrivileges
                .Except(privileges.Select(p => p.Description))
                .ToList();

            if (invalidPrivileges.Any())
            {
                return BadRequest(new
                {
                    Message = "Hay privilegios inválidos",
                    InvalidPrivileges = invalidPrivileges
                });
            }

            string salt = _passwordService.GenerateSalt();
            string passwordHash = _passwordService.HashPassword(password, salt);

            var user = new User
            {
                UserName = userName,
                Email = email,
                Salt = salt,
                PasswordHash = passwordHash,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            foreach (var privilege in privileges)
            {
                _context.UsersPrivileges.Add(new UserPrivilege
                {
                    UserId = user.Id,
                    PrivilegeId = privilege.Id
                });
            }

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Creó el usuario: {user.UserName}"
            );

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.IsActive,
                Privileges = privileges.Select(p => p.Description).ToList()
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            string userName = request.UserName?.Trim() ?? "";
            string email = request.Email?.Trim() ?? "";

            var errorValidacion = ValidarDatosUsuarioSinPassword(userName, email);

            if (errorValidacion != null)
            {
                return BadRequest(errorValidacion);
            }

            bool duplicatedUser = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    (u.UserName == userName || u.Email == email)
                );

            if (duplicatedUser)
            {
                return BadRequest("Ya existe otro usuario con ese nombre o email");
            }

            user.UserName = userName;
            user.Email = email;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Editó el usuario: {user.UserName}"
            );

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.IsActive
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var currentUserName = GetCurrentUserName();

            if (user.UserName == currentUserName)
            {
                return BadRequest("No puede darse de baja el usuario actualmente logueado.");
            }

            user.IsActive = false;

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Dio de baja el usuario: {user.UserName}"
            );

            return Ok("Usuario dado de baja correctamente");
        }

        [HttpGet("privileges")]
        public async Task<IActionResult> GetPrivileges()
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var privileges = await _context.Privileges
                .Select(p => new
                {
                    p.Id,
                    p.Description
                })
                .ToListAsync();

            return Ok(privileges);
        }

        [HttpPost("{id}/privileges")]
        public async Task<IActionResult> AssignPrivileges(int id, AssignPrivilegesRequest request)
        {
            if (!HasPrivilege("USERS_MANAGE"))
            {
                return StatusCode(403, "No tiene privilegios para administrar usuarios");
            }

            var user = await _context.Users
                .Include(u => u.UserPrivileges)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var requestedPrivileges = request.Privileges == null
                ? new List<string>()
                : request.Privileges.Distinct().ToList();

            if (!requestedPrivileges.Any())
            {
                return BadRequest("Debe asignarse al menos un privilegio al usuario.");
            }

            var privileges = await _context.Privileges
                .Where(p => requestedPrivileges.Contains(p.Description))
                .ToListAsync();

            var invalidPrivileges = requestedPrivileges
                .Except(privileges.Select(p => p.Description))
                .ToList();

            if (invalidPrivileges.Any())
            {
                return BadRequest(new
                {
                    Message = "Hay privilegios inválidos",
                    InvalidPrivileges = invalidPrivileges
                });
            }

            var currentPrivileges = await _context.UsersPrivileges
                .Where(up => up.UserId == id)
                .ToListAsync();

            _context.UsersPrivileges.RemoveRange(currentPrivileges);

            foreach (var privilege in privileges)
            {
                _context.UsersPrivileges.Add(new UserPrivilege
                {
                    UserId = id,
                    PrivilegeId = privilege.Id
                });
            }

            await _context.SaveChangesAsync();

            await _auditService.RegistrarAccion(
                GetCurrentUserName(),
                $"Modificó los privilegios del usuario: {user.UserName}"
            );

            return Ok(new
            {
                user.Id,
                user.UserName,
                Privileges = privileges.Select(p => p.Description).ToList()
            });
        }

        private string? ValidarDatosUsuario(string userName, string email, string password)
        {
            var errorUsuario = ValidarDatosUsuarioSinPassword(userName, email);

            if (errorUsuario != null)
            {
                return errorUsuario;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return "La contraseña es obligatoria.";
            }

            if (password.Length < 6)
            {
                return "La contraseña debe tener al menos 6 caracteres.";
            }

            return null;
        }

        private string? ValidarDatosUsuarioSinPassword(string userName, string email)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return "El nombre de usuario es obligatorio.";
            }

            if (userName.Length < 3)
            {
                return "El nombre de usuario debe tener al menos 3 caracteres.";
            }

            if (userName.Length > 30)
            {
                return "El nombre de usuario no puede superar los 30 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return "El email es obligatorio.";
            }

            if (!EmailValido(email))
            {
                return "El email ingresado no tiene un formato válido.";
            }

            return null;
        }

        private bool EmailValido(string email)
        {
            return !string.IsNullOrWhiteSpace(email)
                && email.Contains("@")
                && email.Contains(".");
        }
    }
}