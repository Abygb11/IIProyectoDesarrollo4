//listar y registrar las vacunas de una mascota (módulo de veterinaria).
// RUTAS:    GET  /Vacunas/Index?mascotaId=1
//           POST /Vacunas/Crear
// REGLAS:   - Solo Veterinario y Administrador.
//           - Autoría: AutorId sale del usuario con sesión iniciada, NUNCA del formulario.
//           - La próxima dosis no puede ser anterior a la fecha de aplicación.
//           - Al guardar se actualiza la "última atención" de la mascota.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;
using Veterinaria.Web.ViewModels;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador,Veterinario")]
public class VacunasController : Controller
{
    private readonly AppDbContext _db;
    private readonly AtencionService _atencion;
    public VacunasController(AppDbContext db, AtencionService atencion) { _db = db; _atencion = atencion; }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Arma el modelo de la pantalla (mascota + lista de vacunas + formulario vacío)
    private async Task<VacunasVM?> Armar(int mascotaId)
    {
        var mascota = await _db.Mascotas.FindAsync(mascotaId);
        if (mascota == null) return null;

        return new VacunasVM
        {
            Mascota = mascota,
            Vacunas = await _db.Vacunas
                .Include(v => v.Autor)
                .Where(v => v.MascotaId == mascotaId)
                .OrderByDescending(v => v.FechaAplicacion)
                .ToListAsync(),
            Form = new VacunaForm { MascotaId = mascotaId }
        };
    }

    // Muestra la lista de vacunas de la mascota y el formulario para agregar una nueva vacuna.
    public async Task<IActionResult> Index(int mascotaId)
    {
        var vm = await Armar(mascotaId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Valida y registra la vacuna en la base de datos, luego redirige a la lista de vacunas.
    [HttpPost]
    public async Task<IActionResult> Crear(VacunaForm form)
    {
        if (form.FechaProximaDosis < form.FechaAplicacion)
            ModelState.AddModelError(nameof(form.FechaProximaDosis),
                "La próxima dosis no puede ser anterior a la fecha de aplicación.");

        if (!ModelState.IsValid)
        {
            var vmError = await Armar(form.MascotaId);
            if (vmError == null) return NotFound();
            vmError.Form = form;           // conserva lo que escribió el usuario
            return View("Index", vmError);
        }

        if (!await _db.Mascotas.AnyAsync(m => m.Id == form.MascotaId)) return NotFound();

        _db.Vacunas.Add(new Vacuna
        {
            MascotaId = form.MascotaId,
            Nombre = form.Nombre.Trim(),
            FechaAplicacion = form.FechaAplicacion,
            FechaProximaDosis = form.FechaProximaDosis,
            AutorId = UsuarioId,           // autoría
            FechaRegistro = DateTime.Now
        });
        await _db.SaveChangesAsync();
        await _atencion.RegistrarAsync(form.MascotaId);

        // TempData se usa para mostrar un mensaje de éxito en la siguiente vista, ya que ViewBag no funciona con redirecciones.
        TempData["Ok"] = "Vacuna registrada.";
        return RedirectToAction(nameof(Index), new { mascotaId = form.MascotaId });
    }
}