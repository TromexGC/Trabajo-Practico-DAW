using GestionPeliculas.api.Data;
using GestionPeliculas.api.Models;
using GestionPeliculas.api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT con el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();

    if (!context.Privileges.Any())
    {
        context.Privileges.AddRange(
            new Privilege { Description = "MOVIES_VIEW" },
            new Privilege { Description = "MOVIES_CREATE" },
            new Privilege { Description = "MOVIES_EDIT" },
            new Privilege { Description = "MOVIES_DELETE" },
            new Privilege { Description = "USERS_MANAGE" }
        );

        context.SaveChanges();
    }

    var adminExists = context.Users.Any(u => u.UserName == "admin");

    if (!adminExists)
    {
        string salt = passwordService.GenerateSalt();
        string passwordHash = passwordService.HashPassword("admin123", salt);

        var adminUser = new User
        {
            UserName = "admin",
            Email = "admin@test.com",
            Salt = salt,
            PasswordHash = passwordHash,
            IsActive = true
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        var allPrivileges = context.Privileges.ToList();

        foreach (var privilege in allPrivileges)
        {
            context.UsersPrivileges.Add(new UserPrivilege
            {
                UserId = adminUser.Id,
                PrivilegeId = privilege.Id
            });
        }

        context.SaveChanges();
    }
}

app.Run();