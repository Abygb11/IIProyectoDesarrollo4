// QUÉ HACE: clase que lleva los datos del formulario de registro y edición de mascotas.
//           Incluye los datos opcionales para crear un dueño nuevo si la cédula no existe.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

public class MascotaFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(100)]
    public string Nombre { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona la especie.")]
    public int EspecieId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona la raza.")]
    public int RazaId { get; set; }

    [Required(ErrorMessage = "Indica la fecha de nacimiento."), DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    public Sexo Sexo { get; set; }

    // Foto nueva (opcional) y ruta de la foto actual (solo para mostrarla al editar)
    public IFormFile? Foto { get; set; }
    public string? FotoRuta { get; set; }

    [Required(ErrorMessage = "Indica la cédula del dueño."), StringLength(20)]
    public string CedulaDueno { get; set; } = "";

    // Solo se usan si la cédula no existe: se crea un dueño nuevo
    [StringLength(100)]
    public string? NombreDueno { get; set; }

    [EmailAddress(ErrorMessage = "Correo no válido.")]
    public string? CorreoDueno { get; set; }

    [DataType(DataType.Password)]
    public string? PasswordDueno { get; set; }
}