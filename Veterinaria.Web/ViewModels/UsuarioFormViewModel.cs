using Veterinaria.Web.Models;

namespace Veterinaria.Web.ViewModels;

// Representa los datos del formulario de usuario.
// No es la entidad Usuario porque el formulario trae una "Password" en texto que nunca se guarda tal cual
public class UsuarioFormViewModel
{
    public int Id { get; set; }
    public string Cedula { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string? Password { get; set; }
    public Perfil Perfil { get; set; } = Perfil.Dueno;
    public int? VeterinarioId { get; set; }
}