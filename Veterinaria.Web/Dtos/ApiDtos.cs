namespace Veterinaria.Web.Dtos;

public record LoginRequest(string Correo, string Password);
public record LoginResponse(string Token, DateTime Expira, string Nombre);

public record MascotaDto(int Id, string Nombre, string Especie, string Raza, bool TieneFoto);
public record VacunaDto(int Id, string Nombre, DateTime FechaAplicacion, DateTime FechaProximaDosis, bool Vencida, string Autor);
public record ExamenDto(int Id, string Descripcion, DateTime Fecha, string NombreArchivo, string TipoContenido, string Autor);