using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Web.Data;
using Veterinaria.Web.Models;
using Veterinaria.Web.ViewModels;

namespace Veterinaria.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly AppDbContext _db;
    public UsuariosController(AppDbContext db) => _db = db;

    // Id del administrador que está usando el sistema (viene del claim de la cookie)
    private int UsuarioActualId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Método de listado con búsqueda y filtrado por perfil
    public async Task<IActionResult> Index(string? q, Perfil? perfil)
    {
        var query = _db.Usuarios.Include(u => u.Veterinario).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Cedula.Contains(q) || u.Nombre.Contains(q) || u.Correo.Contains(q));
        if (perfil.HasValue)
            query = query.Where(u => u.Perfil == perfil.Value);

        ViewBag.Q = q;
        ViewBag.Perfiles = Perfiles(perfil);

        return View(await query.OrderBy(u => u.Nombre).ToListAsync());
    }

    // Métodos de creación y edición (comparten validaciones y limpieza de datos)
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarListas();
        return View(new UsuarioFormViewModel());
    }

    // Método de creación: recibe el ViewModel, valida y guarda la entidad Usuario
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioFormViewModel vm)
    {
        ModelState.Clear();
        Limpiar(vm);
        await Validar(vm, null);

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(vm);
        }

        var usuario = new Usuario
        {
            Cedula = vm.Cedula,
            Nombre = vm.Nombre,
            Correo = vm.Correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password!),
            Perfil = vm.Perfil,
            VeterinarioId = vm.Perfil == Perfil.Dueno ? null : vm.VeterinarioId,
            Bloqueado = false
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Usuario agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Método de edición: recibe el ID del usuario y el ViewModel, valida y actualiza la entidad Usuario
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        await CargarListas();
        return View(new UsuarioFormViewModel
        {
            Id = usuario.Id,
            Cedula = usuario.Cedula,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            Perfil = usuario.Perfil,
            VeterinarioId = usuario.VeterinarioId
            // Password queda vacío: nunca se devuelve ni se muestra
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioFormViewModel vm)
    {
        ModelState.Clear();
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        vm.Id = id; // el Id manda la URL, no el formulario
        Limpiar(vm);
        await Validar(vm, id);

        // Un administrador no puede quitarse a sí mismo el perfil de administrador
        if (id == UsuarioActualId() && vm.Perfil != Perfil.Administrador)
            ModelState.AddModelError("Perfil", "No puede cambiar su propio perfil.");

        if (!ModelState.IsValid)
        {
            await CargarListas();
            return View(vm);
        }

        usuario.Cedula = vm.Cedula;
        usuario.Nombre = vm.Nombre;
        usuario.Correo = vm.Correo;
        usuario.Perfil = vm.Perfil;
        usuario.VeterinarioId = vm.Perfil == Perfil.Dueno ? null : vm.VeterinarioId;

        // La contraseña solo cambia si se escribió una nueva
        if (!string.IsNullOrEmpty(vm.Password))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);

        await _db.SaveChangesAsync();

        TempData["Ok"] = "Usuario modificado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Bloquear o desbloquear un usuario (no puede bloquearse a sí mismo)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarBloqueo(int id)
    {
        if (id == UsuarioActualId())
        {
            TempData["Error"] = "No puede bloquearse a sí mismo.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        usuario.Bloqueado = !usuario.Bloqueado;
        await _db.SaveChangesAsync();

        TempData["Ok"] = usuario.Bloqueado
            ? $"Usuario {usuario.Nombre} bloqueado."
            : $"Usuario {usuario.Nombre} desbloqueado.";
        return RedirectToAction(nameof(Index));
    }

    // ELIMINAR (regla: lo que está en uso no se elimina)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (id == UsuarioActualId())
        {
            TempData["Error"] = "No puede eliminar su propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        // En uso: es dueño de mascotas o autor de algún registro del expediente
        var enUso =
            await _db.Mascotas.AnyAsync(m => m.DuenoId == id) ||
            await _db.Vacunas.AnyAsync(v => v.AutorId == id) ||
            await _db.MascotaItems.AnyAsync(i => i.AutorInicioId == id || i.AutorSuspensionId == id) ||
            await _db.Examenes.AnyAsync(e => e.AutorId == id) ||
            await _db.Notas.AnyAsync(n => n.AutorId == id);

        if (enUso)
        {
            TempData["Error"] = "No se puede eliminar: el usuario tiene mascotas o registros en expedientes. Puede bloquearlo en su lugar.";
            return RedirectToAction(nameof(Index));
        }

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Usuario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    //  Métodos auxiliares 

    // Opciones del <select> de perfil 
    private static List<SelectListItem> Perfiles(Perfil? seleccionado = null) => new()
    {
        new("Administrador", nameof(Perfil.Administrador), seleccionado == Perfil.Administrador),
        new("Veterinario",   nameof(Perfil.Veterinario),   seleccionado == Perfil.Veterinario),
        new("Dueño",         nameof(Perfil.Dueno),         seleccionado == Perfil.Dueno)
    };

    private async Task CargarListas()
    {
        ViewBag.Perfiles = Perfiles();
        var veterinarios = await _db.Veterinarios.OrderBy(v => v.NombreCompleto).ToListAsync();
        ViewBag.Veterinarios = new SelectList(veterinarios, "Id", "NombreCompleto");
    }

    private static void Limpiar(UsuarioFormViewModel vm)
    {
        vm.Cedula = (vm.Cedula ?? "").Trim();
        vm.Nombre = (vm.Nombre ?? "").Trim();
        vm.Correo = (vm.Correo ?? "").Trim();
        // La contraseña NO se recorta: los espacios pueden ser parte de ella
    }

    // Validaciones compartidas por Crear y Editar
    private async Task Validar(UsuarioFormViewModel vm, int? idActual)
    {
        var esNuevo = idActual == null;

        if (string.IsNullOrWhiteSpace(vm.Cedula))
            ModelState.AddModelError("Cedula", "La cédula es obligatoria.");
        else if (await _db.Usuarios.AnyAsync(u => u.Cedula == vm.Cedula && u.Id != idActual))
            ModelState.AddModelError("Cedula", "Ya existe un usuario con esa cédula.");

        if (string.IsNullOrWhiteSpace(vm.Nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(vm.Correo))
            ModelState.AddModelError("Correo", "El correo electrónico es obligatorio.");
        else if (!new EmailAddressAttribute().IsValid(vm.Correo))
            ModelState.AddModelError("Correo", "El correo electrónico no tiene un formato válido.");
        else if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.Correo && u.Id != idActual))
            ModelState.AddModelError("Correo", "Ya existe un usuario con ese correo.");

        if (!Enum.IsDefined(typeof(Perfil), vm.Perfil))
            ModelState.AddModelError("Perfil", "Seleccione un perfil válido.");

        if (esNuevo && string.IsNullOrEmpty(vm.Password))
            ModelState.AddModelError("Password", "La contraseña es obligatoria.");
        else if (!string.IsNullOrEmpty(vm.Password) && vm.Password.Length < 8)
            ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres.");

        if (vm.Perfil == Perfil.Veterinario && vm.VeterinarioId == null)
            ModelState.AddModelError("VeterinarioId", "Debe seleccionar el veterinario vinculado.");
        else if (vm.VeterinarioId != null && vm.Perfil != Perfil.Dueno &&
                 !await _db.Veterinarios.AnyAsync(v => v.Id == vm.VeterinarioId))
            ModelState.AddModelError("VeterinarioId", "El veterinario seleccionado no existe.");
    }
}