// Modelo de un resultado de examen, tal como lo devuelve GET /api/mascotas/{id}/examenes.
class Examen {
  final int id;
  final String descripcion;
  final DateTime fecha;
  final String nombreArchivo; // por ejemplo "hemograma.pdf"
  final String tipoContenido; // "application/pdf", "image/jpeg" o "image/png"
  final String autor; // veterinario que lo registró

  Examen({
    required this.id,
    required this.descripcion,
    required this.fecha,
    required this.nombreArchivo,
    required this.tipoContenido,
    required this.autor,
  });

  // true si el archivo es un PDF; si no, es una imagen. Sirve para elegir el ícono.
  bool get esPdf => tipoContenido == 'application/pdf';

  // Convierte un objeto JSON de la API en un Examen.
  factory Examen.fromJson(Map<String, dynamic> json) {
    return Examen(
      id: json['id'] as int,
      descripcion: json['descripcion'] ?? '',
      fecha: DateTime.parse(json['fecha']),
      nombreArchivo: json['nombreArchivo'] ?? '',
      tipoContenido: json['tipoContenido'] ?? '',
      autor: json['autor'] ?? '',
    );
  }
}
