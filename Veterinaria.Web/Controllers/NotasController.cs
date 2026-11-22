// QUÉ HACE: historial clínico de la mascota (notas con texto enriquecido).
// RUTAS:    GET  /Notas/Index?mascotaId=1
//           POST /Notas/Crear
// REGLAS:   - Solo Veterinario y Administrador.
//           - Las notas NO se pueden modificar ni eliminar: este controlador no tiene
//             acciones de editar ni de eliminar.
//           - El HTML del editor se sanitiza antes de guardarlo.
//           - Autoría: AutorId sale del usuario con sesión iniciada, NUNCA del formulario.
//           - Al guardar se actualiza la "última atención" de la mascota.
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.Services;
using Veterinaria.Web.ViewModels;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador,Veterinario")]
public class NotasController : Controller
{
    private readonly AppDbContext _db;
    private readonly AtencionService _atencion;
    public NotasController(AppDbContext db, AtencionService atencion) { _db = db; _atencion = atencion; }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Arma el modelo de la pantalla (mascota + notas de la más reciente a la más antigua + formulario vacío)
    private async Task<NotasVM?> Armar(int mascotaId)
    {
        var mascota = await _db.Mascotas.FindAsync(mascotaId);
        if (mascota == null) return null;

        return new NotasVM
        {
            Mascota = mascota,
            Notas = await _db.Notas
                .Include(n => n.Autor)
                .Where(n => n.MascotaId == mascotaId)
                .OrderByDescending(n => n.FechaHora)
                .ToListAsync(),
            Form = new NotaForm { MascotaId = mascotaId }
        };
    }

    // Muestra el historial de la mascota y el editor para agregar una nota nueva.
    public async Task<IActionResult> Index(int mascotaId)
    {
        var vm = await Armar(mascotaId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Sanitiza el HTML, valida que la nota no esté vacía y la guarda con fecha, hora y autor.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NotaForm form)
    {
        // Quita scripts, eventos y etiquetas peligrosas del HTML que envía el navegador
        var limpio = new HtmlSanitizer().Sanitize(form.ContenidoHtml ?? "");

        // Texto sin etiquetas, para detectar notas vacías (el editor vacío genera <p><br></p>)
        var texto = WebUtility.HtmlDecode(Regex.Replace(limpio, "<.*?>", ""));
        if (string.IsNullOrWhiteSpace(texto))
            ModelState.AddModelError(string.Empty, "Escribe la nota del historial.");

        if (!ModelState.IsValid)
        {
            var vmError = await Armar(form.MascotaId);
            if (vmError == null) return NotFound();
            vmError.Form = form;           // conserva lo que escribió el usuario
            return View("Index", vmError);
        }

        if (!await _db.Mascotas.AnyAsync(m => m.Id == form.MascotaId)) return NotFound();

        _db.Notas.Add(new NotaClinica
        {
            MascotaId = form.MascotaId,
            ContenidoHtml = limpio,        // se guarda ya sanitizado
            FechaHora = DateTime.Now,
            AutorId = UsuarioId            // autoría
        });
        await _db.SaveChangesAsync();
        await _atencion.RegistrarAsync(form.MascotaId);

        TempData["Ok"] = "Nota agregada al historial.";
        return RedirectToAction(nameof(Index), new { mascotaId = form.MascotaId });
    }
}