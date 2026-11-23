using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Veterinaria.Web.Data;

namespace Veterinaria.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(nameof(Perfil.Dueno)))
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var mascotas = await _db.Mascotas
                .Where(m => m.DuenoId == usuarioId)
                .Include(m => m.Especie)
                .Include(m => m.Raza)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View(mascotas);
        }

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
