using GestionPeliculas.api.Data;
using GestionPeliculas.api.DTOs;
using GestionPeliculas.api.Models;
using GestionPeliculas.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionPeliculas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;

        public UsersController(AppDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
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

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return BadRequest("El nombre de usuario es obligatorio");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("El email es obligatorio");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("La contraseña es obligatoria");
            }

            bool userExists = await _context.Users
                .AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email);

            if (userExists)
            {
                return BadRequest("Ya existe un usuario con ese nombre o email");
            }

            string salt = _passwordService.GenerateSalt();
            string passwordHash = _passwordService.HashPassword(request.Password, salt);

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                Salt = salt,
                PasswordHash = passwordHash,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var requestedPrivileges = request.Privileges
                .Distinct()
                .ToList();

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

            foreach (var privilege in privileges)
            {
                _context.UsersPrivileges.Add(new UserPrivilege
                {
                    UserId = user.Id,
                    PrivilegeId = privilege.Id
                });
            }

            await _context.SaveChangesAsync();

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

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return BadRequest("El nombre de usuario es obligatorio");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("El email es obligatorio");
            }

            bool duplicatedUser = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    (u.UserName == request.UserName || u.Email == request.Email)
                );

            if (duplicatedUser)
            {
                return BadRequest("Ya existe otro usuario con ese nombre o email");
            }

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

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

            user.IsActive = false;

            await _context.SaveChangesAsync();

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

            var requestedPrivileges = request.Privileges
                .Distinct()
                .ToList();

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

            return Ok(new
            {
                user.Id,
                user.UserName,
                Privileges = privileges.Select(p => p.Description).ToList()
            });
        }
    }
}