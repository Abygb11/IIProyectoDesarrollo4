// QUÉ HACE: clases que llevan datos entre los controladores y las vistas
//           (registro de dueños, vacunas y medicamentos/tratamientos del expediente).
using System.ComponentModel.DataAnnotations;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

// ---------- Registro de dueños ----------
public class RegistroVM
{
    [Required(ErrorMessage = "La cédula es obligatoria."), StringLength(20)]
    public string Cedula { get; set; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "Correo no válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria."), MinLength(6, ErrorMessage = "Mínimo 6 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = "";
}

// ---------- Vacunas ----------
public class VacunaForm
{
    public int MascotaId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre de la vacuna."), StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Indica la fecha de aplicación."), DataType(DataType.Date)]
    public DateTime FechaAplicacion { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Indica la fecha de la próxima dosis."), DataType(DataType.Date)]
    public DateTime FechaProximaDosis { get; set; } = DateTime.Today.AddYears(1);
}

public class VacunasVM
{
    public Mascota Mascota { get; set; } = null!;
    public List<Vacuna> Vacunas { get; set; } = new();
    public VacunaForm Form { get; set; } = new();
}

// ---------- Medicamentos y tratamientos ----------
public class ItemForm
{
    public int MascotaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un medicamento o tratamiento.")]
    public int ItemClinicoId { get; set; }
}

public class ItemsVM
{
    public Mascota Mascota { get; set; } = null!;
    public List<MascotaItem> Vigentes { get; set; } = new();
    public List<MascotaItem> Suspendidos { get; set; } = new();
    public List<ItemClinico> Catalogo { get; set; } = new();
    public ItemForm Form { get; set; } = new();
}