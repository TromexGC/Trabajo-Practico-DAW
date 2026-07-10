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
            if (string.IsNullOrWhiteSpace(request.UserName))
                return BadRequest("El nombre de usuario es obligatorio");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("El email es obligatorio");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("La contraseña es obligatoria");

            bool userExists = await _context.Users
                .AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email);

            if (userExists)
                return BadRequest("Ya existe un usuario con ese nombre o email");

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
            var user = await _context.Users
                .Include(u => u.UserPrivileges)
                .ThenInclude(up => up.Privilege)
                .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive);

            if (user == null)
                return Unauthorized("Usuario o contraseña incorrectos");

            bool validPassword = _passwordService.VerifyPassword(
                request.Password,
                user.Salt,
                user.PasswordHash
            );

            if (!validPassword)
                return Unauthorized("Usuario o contraseña incorrectos");

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
                return Unauthorized("Refresh token inválido o expirado");

            if (!storedRefreshToken.User.IsActive)
                return Unauthorized("Usuario inactivo");

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
    }
}