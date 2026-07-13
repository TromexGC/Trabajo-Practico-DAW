using GestionPeliculas.api.Data;
using GestionPeliculas.api.DTOs;
using GestionPeliculas.api.Models;
using GestionPeliculas.api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionPeliculas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly TokenService _tokenService;

        public AuthController(
            AppDbContext context,
            PasswordService passwordService,
            TokenService tokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
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

            var viewPrivilege = await _context.Privileges
                .FirstOrDefaultAsync(p => p.Description == "MOVIES_VIEW");

            if (viewPrivilege != null)
            {
                _context.UsersPrivileges.Add(new UserPrivilege
                {
                    UserId = user.Id,
                    PrivilegeId = viewPrivilege.Id
                });

                await _context.SaveChangesAsync();
            }

            var privileges = await _context.UsersPrivileges
                .Where(up => up.UserId == user.Id)
                .Select(up => up.Privilege.Description)
                .ToListAsync();

            string token = _tokenService.GenerateAccessToken(user, privileges);
            string refreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                UserName = user.UserName,
                Privileges = privileges
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            string userName = request.UserName?.Trim() ?? "";
            string password = request.Password ?? "";

            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest("El nombre de usuario es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest("La contraseña es obligatoria.");
            }

            var user = await _context.Users
                .Include(u => u.UserPrivileges)
                .ThenInclude(up => up.Privilege)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive);

            if (user == null)
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            bool validPassword = _passwordService.VerifyPassword(
                password,
                user.Salt,
                user.PasswordHash
            );

            if (!validPassword)
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            var privileges = user.UserPrivileges
                .Select(up => up.Privilege.Description)
                .ToList();

            string token = _tokenService.GenerateAccessToken(user, privileges);
            string refreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                UserName = user.UserName,
                Privileges = privileges
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("El refresh token es obligatorio.");
            }

            var storedRefreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.UserPrivileges)
                .ThenInclude(up => up.Privilege)
                .FirstOrDefaultAsync(rt =>
                    rt.Token == request.RefreshToken &&
                    !rt.IsRevoked &&
                    rt.Expires > DateTime.UtcNow
                );

            if (storedRefreshToken == null)
            {
                return Unauthorized("Refresh token inválido o expirado");
            }

            if (!storedRefreshToken.User.IsActive)
            {
                return Unauthorized("Usuario inactivo");
            }

            storedRefreshToken.IsRevoked = true;

            var privileges = storedRefreshToken.User.UserPrivileges
                .Select(up => up.Privilege.Description)
                .ToList();

            string newToken = _tokenService.GenerateAccessToken(storedRefreshToken.User, privileges);
            string newRefreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = storedRefreshToken.UserId,
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                UserName = storedRefreshToken.User.UserName,
                Privileges = privileges
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            string email = request.Email?.Trim() ?? "";
            string newPassword = request.NewPassword ?? "";
            string confirmPassword = request.ConfirmPassword ?? "";

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                return BadRequest("Todos los campos son obligatorios.");
            }

            if (!EmailValido(email))
            {
                return BadRequest("El email ingresado no tiene un formato válido.");
            }

            if (newPassword.Length < 6)
            {
                return BadRequest("La contraseña debe tener al menos 6 caracteres.");
            }

            if (newPassword != confirmPassword)
            {
                return BadRequest("Las contraseñas no coinciden.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                return NotFound("No existe un usuario activo con ese email.");
            }

            var salt = _passwordService.GenerateSalt();
            var hash = _passwordService.HashPassword(newPassword, salt);

            user.Salt = salt;
            user.PasswordHash = hash;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Contraseña actualizada correctamente."
            });
        }

        private string? ValidarDatosUsuario(string userName, string email, string password)
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

        private bool EmailValido(string email)
        {
            return !string.IsNullOrWhiteSpace(email)
                && email.Contains("@")
                && email.Contains(".");
        }
    }
}