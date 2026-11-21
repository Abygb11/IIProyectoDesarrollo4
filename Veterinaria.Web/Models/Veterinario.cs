namespace Veterinaria.Web.Models;

public class Veterinario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string NumeroColegiado { get; set; } = "";
    public string? FotoRuta { get; set; }
}