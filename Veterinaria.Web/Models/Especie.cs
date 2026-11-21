namespace Veterinaria.Web.Models;

public class Especie
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public List<Raza> Razas { get; set; } = new();
}