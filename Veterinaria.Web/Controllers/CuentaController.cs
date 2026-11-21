using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;

namespace Veterinaria.Web.Controllers;

public class CuentaController : Controller
{
    private readonly AppDbContext _db;
    public CuentaController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Login() => View();

    // Se realiza el inicio de sesión, utilizando claims y cookies para la autenticación.
    [HttpPost]
    public async Task<IActionResult> Login(string correo, string password)
    {
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Correo == correo);

        // Se valida que el usuario exista y que la contraseña sea correcta.
        if (u == null || !BCrypt.Net.BCrypt.Verify(password, u.PasswordHash))
        {
            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View();
        }
        // Se valida que el usuario no esté bloqueado.
        if (u.Bloqueado)
        {
            ViewBag.Error = "Tu usuario está bloqueado. Contacta a un administrador.";
            return View();
        }

        // Un claim es una afirmación sobre un usuario, entonces se crean esas afirmaciones
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Role, u.Perfil.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

   // Se realiza el cierre de sesión, eliminando la cookie de autenticación.
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
    // Vista que se muestra cuando un usuario intenta acceder a una página para la cual no tiene permisos.
    public IActionResult Denegado() => View();
}