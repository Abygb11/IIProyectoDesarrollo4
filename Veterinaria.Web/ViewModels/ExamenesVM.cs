// Clases que llevan los datos de la pantalla de exámenes entre el controlador y la vista.
using System.ComponentModel.DataAnnotations;
using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

public class ExamenForm
{
    public int MascotaId { get; set; }

    [Required(ErrorMessage = "Escriba una breve descripción del examen."),
     StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string Descripcion { get; set; } = "";
}

public class ExamenesVM
{
    public Mascota Mascota { get; set; } = null!;
    public List<Examen> Examenes { get; set; } = new();
    public ExamenForm Form { get; set; } = new();
}