// Modelo de una mascota del dueño, tal como la devuelve GET /api/mascotas.
class Mascota {
  final int id;
  final String nombre;
  final String especie;
  final String raza;
  final bool tieneFoto; // si es false, la app no pide la foto a la API

  Mascota({
    required this.id,
    required this.nombre,
    required this.especie,
    required this.raza,
    required this.tieneFoto,
  });

  // Convierte un objeto JSON de la API en una Mascota, si algún texto llega null, se usa '' para que la app no se caiga.
  factory Mascota.fromJson(Map<String, dynamic> json) {
    return Mascota(
      id: json['id'] as int,
      nombre: json['nombre'] ?? '',
      especie: json['especie'] ?? '',
      raza: json['raza'] ?? '',
      tieneFoto: json['tieneFoto'] ?? false,
    );
  }
}
