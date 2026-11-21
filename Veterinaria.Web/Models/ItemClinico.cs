namespace Veterinaria.Web.Models;

public class ItemClinico
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public TipoItem Tipo { get; set; }
    public string Descripcion { get; set; } = "";
}