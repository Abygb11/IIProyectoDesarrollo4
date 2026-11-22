using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Dtos;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;

namespace Veterinaria.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    public AuthApiController(AppDbContext db, JwtService jwt) { _db = db; _jwt = jwt; }

    // esto es para que la app móvil pueda iniciar sesión y obtener un token JWT que se usará en las demás llamadas a la API
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Correo == req.Correo);

        if (u == null || !BCrypt.Net.BCrypt.Verify(req.Password, u.PasswordHash))
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });

        if (u.Bloqueado)
            return StatusCode(403, new { mensaje = "Usuario bloqueado." });

        if (u.Perfil != Perfil.Dueno)
            return StatusCode(403, new { mensaje = "La aplicación móvil es solo para dueños." });

        var (token, expira) = _jwt.Generar(u);
        return Ok(new LoginResponse(token, expira, u.Nombre));
    }
}