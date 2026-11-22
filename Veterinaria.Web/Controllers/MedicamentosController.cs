// asignar y suspender medicamentos/tratamientos del catálogo clínico a una mascota.
//           Muestra por separado los VIGENTES y los SUSPENDIDOS.
// RUTAS:    GET  /Medicamentos/Index?mascotaId=1
//           POST /Medicamentos/Agregar
//           POST /Medicamentos/Suspender/5
// REGLAS:   - Solo Veterinario y Administrador.
//           - Vigente = FechaSuspension == null. Suspender guarda fecha y autor.
//           - No se asigna dos veces el mismo ítem mientras esté vigente.
//           - Autoría de inicio y de suspensión sale del usuario con sesión.
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
public class MedicamentosController : Controller
{
    private readonly AppDbContext _db;
    private readonly AtencionService _atencion;
    public MedicamentosController(AppDbContext db, AtencionService atencion) { _db = db; _atencion = atencion; }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Arma el modelo de la pantalla (mascota + lista de ítems clínicos + formulario vacío)
    private async Task<ItemsVM?> Armar(int mascotaId)
    {
        var mascota = await _db.Mascotas.FindAsync(mascotaId);
        if (mascota == null) return null;

        var items = await _db.MascotaItems
            .Include(i => i.ItemClinico)
            .Include(i => i.AutorInicio)
            .Include(i => i.AutorSuspension)
            .Where(i => i.MascotaId == mascotaId)
            .OrderByDescending(i => i.FechaInicio)
            .ToListAsync();

        return new ItemsVM
        {
            Mascota = mascota,
            Vigentes = items.Where(i => i.FechaSuspension == null).ToList(),
            Suspendidos = items.Where(i => i.FechaSuspension != null).ToList(),
            Catalogo = await _db.ItemsClinicos.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync(),
            Form = new ItemForm { MascotaId = mascotaId }
        };
    }

    // Muestra la lista de medicamentos/tratamientos de la mascota y el formulario para agregar un nuevo ítem clínico.
    public async Task<IActionResult> Index(int mascotaId)
    {
        var vm = await Armar(mascotaId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Valida y registra el ítem clínico en la base de datos, luego redirige a la lista de medicamentos/tratamientos.
    [HttpPost]
    public async Task<IActionResult> Agregar(ItemForm form)
    {
        if (ModelState.IsValid &&
            await _db.MascotaItems.AnyAsync(i => i.MascotaId == form.MascotaId
                                              && i.ItemClinicoId == form.ItemClinicoId
                                              && i.FechaSuspension == null))
        {
            ModelState.AddModelError(nameof(form.ItemClinicoId), "Ese medicamento o tratamiento ya está vigente.");
        }

        if (!ModelState.IsValid)
        {
            var vmError = await Armar(form.MascotaId);
            if (vmError == null) return NotFound();
            vmError.Form = form;
            return View("Index", vmError);
        }

        _db.MascotaItems.Add(new MascotaItem
        {
            MascotaId = form.MascotaId,
            ItemClinicoId = form.ItemClinicoId,
            FechaInicio = DateTime.Now,
            AutorInicioId = UsuarioId  // autoría de inicio
        });
        await _db.SaveChangesAsync();
        await _atencion.RegistrarAsync(form.MascotaId);

        TempData["Ok"] = "Agregado al expediente.";
        return RedirectToAction(nameof(Index), new { mascotaId = form.MascotaId });
    }

    // Suspende un ítem clínico vigente, guardando la fecha y el autor de la suspensión.
    [HttpPost]
    public async Task<IActionResult> Suspender(int id)
    {
        var item = await _db.MascotaItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return NotFound();

        if (item.FechaSuspension == null)  // solo se suspende si está vigente
        {
            item.FechaSuspension = DateTime.Now;
            item.AutorSuspensionId = UsuarioId; // autoría de suspensión
            await _db.SaveChangesAsync();
            await _atencion.RegistrarAsync(item.MascotaId);
            TempData["Ok"] = "Suspendido correctamente.";
        }

        // Si ya estaba suspendido, no hace nada, solo redirige a la lista.
        return RedirectToAction(nameof(Index), new { mascotaId = item.MascotaId });
    }
}