import 'dart:async';
import 'dart:convert'; // jsonEncode y jsonDecode
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config.dart';
import 'errores.dart';
import 'sesion_service.dart';

// Todas las llamadas a la API de la veterinaria pasan por esta clase.
class ApiService {
  // Si la API no responde en este tiempo, se toma como sin conexión.
  static const tiempoEspera = Duration(seconds: 15);

  // Encabezado con el token JWT. Se manda en CADA petición (también lo usa la foto).
  static Map<String, String> get encabezados => {
        'Authorization': 'Bearer ${SesionService.token}',
      };

  // GET a una ruta de la API (por ejemplo '/api/mascotas'). Si todo sale bien devuelve la respuesta; si no, lanza uno de los errores de errores.dart.
  static Future<http.Response> _get(String ruta) async {
    final http.Response respuesta;
    try {
      respuesta = await http
          .get(Uri.parse('$baseUrl$ruta'), headers: encabezados)
          .timeout(tiempoEspera);
    } on SocketException {
      throw SinConexionException(); // sin internet o no se encontró el servidor
    } on TimeoutException {
      throw SinConexionException(); // el servidor tardó más de 15 segundos
    } on http.ClientException {
      throw SinConexionException(); // la conexión se cortó a medias
    }

    if (respuesta.statusCode == 200) {
      return respuesta;
    }
    if (respuesta.statusCode == 401) {
      // El token venció o no es válido y se borra la sesión para volver al login.
      await SesionService.borrarSesion();
      throw SesionExpiradaException();
    }
    if (respuesta.statusCode == 404) {
      throw ApiException('No se encontró la información.');
    }
    throw ApiException('Error del servidor (${respuesta.statusCode}). Intenta más tarde.');
  }

  // Inicia sesión con el correo y la contraseña del dueño. Si sale bien, guarda la sesión.
  // Va aparte de _get porque aquí un 401 significa correo o contraseña incorrectos, no sesión expirada.
  static Future<void> login(String correo, String password) async {
    final http.Response respuesta;
    try {
      respuesta = await http
          .post(
            Uri.parse('$baseUrl/api/auth/login'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({'correo': correo, 'password': password}),
          )
          .timeout(tiempoEspera);
    } on SocketException {
      throw SinConexionException();
    } on TimeoutException {
      throw SinConexionException();
    } on http.ClientException {
      throw SinConexionException();
    }

    if (respuesta.statusCode == 200) {
      // {"token":"eyJ...","expira":"2026-11-22T22:17:13Z","nombre":"María Rojas"}
      final datos = jsonDecode(respuesta.body);
      await SesionService.guardarSesion(
        datos['token'],
        datos['nombre'],
        DateTime.parse(datos['expira']),
      );
      return;
    }
    if (respuesta.statusCode == 401 || respuesta.statusCode == 403) {
      // La API manda el motivo: "mensaje":"Usuario bloqueado."
      final datos = jsonDecode(respuesta.body);
      throw ApiException(datos['mensaje'] ?? 'No se pudo iniciar sesión.');
    }
    throw ApiException('Error del servidor (${respuesta.statusCode}). Intenta más tarde.');
  }
}
