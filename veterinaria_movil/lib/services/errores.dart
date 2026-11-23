// El servicio de la API los lanza y las pantallas revisan de qué tipo es cada uno para mostrar el mensaje correcto.

// No hay internet, el servidor no responde o se pasó el tiempo de espera.
class SinConexionException implements Exception {}

// La API respondió 401: el token venció o no es válido. Hay que volver al login.
class SesionExpiradaException implements Exception {}

// Cualquier otro error con un mensaje para mostrarle al usuario (credenciales incorrectas, usuario bloqueado et).
class ApiException implements Exception {
  final String mensaje;

  ApiException(this.mensaje);

  @override
  String toString() => mensaje;
}
