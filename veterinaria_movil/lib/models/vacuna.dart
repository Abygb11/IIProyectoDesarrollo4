// Modelo de una vacuna aplicada, tal como la devuelve GET /api/mascotas/{id}/vacunas.
class Vacuna {
  final int id;
  final String nombre;
  final DateTime fechaAplicacion;
  final DateTime fechaProximaDosis;
  final bool vencida; // la calcula el servidor y la app solo la pinta en rojo
  final String autor; // veterinario que la aplicó

  Vacuna({
    required this.id,
    required this.nombre,
    required this.fechaAplicacion,
    required this.fechaProximaDosis,
    required this.vencida,
    required this.autor,
  });

  // Convierte un objeto JSON de la API en una Vacuna.
  // Las fechas llegan como texto ("2026-05-21T00:25:04.0333333") y DateTime.parse las convierte.
  factory Vacuna.fromJson(Map<String, dynamic> json) {
    return Vacuna(
      id: json['id'] as int,
      nombre: json['nombre'] ?? '',
      fechaAplicacion: DateTime.parse(json['fechaAplicacion']),
      fechaProximaDosis: DateTime.parse(json['fechaProximaDosis']),
      vencida: json['vencida'] ?? false,
      autor: json['autor'] ?? '',
    );
  }
}
