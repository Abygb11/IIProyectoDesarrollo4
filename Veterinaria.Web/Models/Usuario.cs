namespace Veterinaria.Web.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Cedula { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Perfil Perfil { get; set; }
    public bool Bloqueado { get; set; }
    public int? VeterinarioId { get; set; }
    public Veterinario? Veterinario { get; set; }
}