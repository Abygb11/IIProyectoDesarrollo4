using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace Veterinaria.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    // Veterinarios y administradores van directo al listado de mascotas al iniciar sesión.
    public IActionResult Index()
    {
        if (User.IsInRole("Administrador") || User.IsInRole("Veterinario"))
            return RedirectToAction("Index", "Mascotas");

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
