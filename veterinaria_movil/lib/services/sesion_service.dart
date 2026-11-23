import 'package:flutter_secure_storage/flutter_secure_storage.dart';

// Guarda la sesión del dueño (token JWT, nombre y fecha en que vence el token).
// Se guarda cifrada en el celular para que siga abierta al cerrar la app y también en memoria para no leer el almacenamiento en cada petición.
class SesionService {
  static const _storage = FlutterSecureStorage();

  // Datos de la sesión en memoria.
  static String? token;
  static String? nombre;
  static DateTime? expira;

  // Se llama después de un login correcto.
  static Future<void> guardarSesion(String nuevoToken, String nuevoNombre, DateTime nuevaExpira) async {
    token = nuevoToken;
    nombre = nuevoNombre;
    expira = nuevaExpira;
    await _storage.write(key: 'token', value: nuevoToken);
    await _storage.write(key: 'nombre', value: nuevoNombre);
    await _storage.write(key: 'expira', value: nuevaExpira.toIso8601String());
  }

  // Se llama al abrir la app, pasa a memoria la sesión que quedó guardada.
  static Future<void> leerSesion() async {
    token = await _storage.read(key: 'token');
    nombre = await _storage.read(key: 'nombre');
    final textoExpira = await _storage.read(key: 'expira');
    expira = textoExpira == null ? null : DateTime.parse(textoExpira);
  }

  // Se llama al cerrar sesión o cuando la API responde 401.
  static Future<void> borrarSesion() async {
    token = null;
    nombre = null;
    expira = null;
    await _storage.deleteAll();
  }

  // true si hay token y todavía no ha vencido (el token dura 1 hora).
  static bool get haySesionValida {
    if (token == null || expira == null) return false;
    return DateTime.now().isBefore(expira!);
  }
}
