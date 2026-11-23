import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../config.dart';
import '../models/examen.dart';
import '../models/mascota.dart';
import '../models/vacuna.dart';
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

  // Mascotas del dueño que inició sesión 
  static Future<List<Mascota>> getMascotas() async {
    final respuesta = await _get('/api/mascotas');
    final List lista = jsonDecode(respuesta.body);
    return lista.map((json) => Mascota.fromJson(json)).toList();
  }

  // Vacunas aplicadas a una mascota.
  static Future<List<Vacuna>> getVacunas(int mascotaId) async {
    final respuesta = await _get('/api/mascotas/$mascotaId/vacunas');
    final List lista = jsonDecode(respuesta.body);
    return lista.map((json) => Vacuna.fromJson(json)).toList();
  }

  // Resultados de exámenes de una mascota 
  static Future<List<Examen>> getExamenes(int mascotaId) async {
    final respuesta = await _get('/api/mascotas/$mascotaId/examenes');
    final List lista = jsonDecode(respuesta.body);
    return lista.map((json) => Examen.fromJson(json)).toList();
  }

  // Descarga el archivo de un examen (PDF o imagen) y lo abre con una app del celular.
  static Future<void> descargarExamen(int mascotaId, Examen examen) async {
    // 1. Pedir el archivo a la API (con token y manejo de errores, igual que todo).
    final respuesta = await _get('/api/mascotas/$mascotaId/examenes/${examen.id}/archivo');

    // 2. Guardarlo en la carpeta temporal de la app. El id va al inicio para que dos exámenes con el mismo nombre de archivo no se pisen.
    final carpeta = await getTemporaryDirectory();
    final archivo = File('${carpeta.path}/${examen.id}_${examen.nombreArchivo}');
    await archivo.writeAsBytes(respuesta.bodyBytes); // bodyBytes: el archivo tal cual, sin pasarlo a texto

    // 3. Abrirlo. tipoContenido le dice a Android si es PDF o imagen para elegir la app.
    final resultado = await OpenFilex.open(archivo.path, type: examen.tipoContenido);
    if (resultado.type != ResultType.done) {
      throw ApiException('No hay una aplicación para abrir este archivo.');
    }
  }
}
