using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models; 
using Veterinaria.Web.ViewModels;

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

    // Vista para registrar un nuevo usuario (dueño de mascota) desde la web. La app móvil no tiene registro, solo inicio de sesión.
    [HttpGet]
    public IActionResult Registro() => View(new RegistroVM());

    // Se realiza el registro de un nuevo usuario (dueño de mascota) desde la web. La app móvil no tiene registro, solo inicio de sesión.
    [HttpPost]
    public async Task<IActionResult> Registro(RegistroVM vm)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Cedula == vm.Cedula))
            ModelState.AddModelError(nameof(vm.Cedula), "Ya existe un usuario con esa cédula.");
        if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.Correo))
            ModelState.AddModelError(nameof(vm.Correo), "Ya existe un usuario con ese correo.");

        if (!ModelState.IsValid) return View(vm);

        _db.Usuarios.Add(new Usuario
        {
            Cedula = vm.Cedula.Trim(),
            Nombre = vm.Nombre.Trim(),
            Correo = vm.Correo.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
            Perfil = Perfil.Dueno, // fijo, no viene del formulario
            Bloqueado = false
        });
        await _db.SaveChangesAsync();

        // TempData se usa para enviar un mensaje a la vista de Login, que se mostrará una sola vez.
        TempData["Ok"] = "Cuenta creada. Ya puedes iniciar sesión desde la app móvil.";
        return RedirectToAction("Login");
    }
}