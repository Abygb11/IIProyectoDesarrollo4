import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

// Colores, textos y tema de toda la app en un solo lugar.
// Si se cambia algo aqui, cambia en todas las pantallas.
class Estilos {
  // Colores: la app usa rojo vino como color principal
  static const Color primario = Color(0xFFA4161A); // rojo vino
  static const Color fondo = Color(0xFFFFF8F6); // fondo de las pantallas, casi blanco
  static const Color fondoIcono = Color(0xFFFDECEC); // rosado claro detras de los iconos
  static const Color bordeTarjeta = Color(0xFFF0D5D5);
  static const Color vencida = Color(0xFFE53935); // rojo vivo, distinto del vino
  static final Color alDia = Colors.green.shade700;
  static const Color pdf = Color(0xFFE53935);
  static final Color imagen = Colors.indigo.shade400;
  static const Color gris = Colors.grey;

  // Icono de la app (un conejo) en vez de la huella
  static const IconData iconoAnimal = Icons.cruelty_free;

  // Textos
  static const TextStyle titulo = TextStyle(fontSize: 18, fontWeight: FontWeight.bold);
  static const TextStyle negrita = TextStyle(fontWeight: FontWeight.bold);
  static const TextStyle secundario = TextStyle(color: gris);

  // Fechas: 21/05/2026
  static final DateFormat formatoFecha = DateFormat('dd/MM/yyyy');

  // Tema que usa MaterialApp en main.dart
  static ThemeData get tema {
    return ThemeData(
      colorScheme: .fromSeed(seedColor: primario, primary: primario),
      scaffoldBackgroundColor: fondo,
      // Barra de arriba rojo vino, letras blancas y titulo centrado
      appBarTheme: const AppBarTheme(
        backgroundColor: primario,
        foregroundColor: Colors.white,
        centerTitle: true,
      ),
      // Tarjetas blancas, planas, con borde suave y esquinas redondeadas
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        margin: const EdgeInsets.symmetric(vertical: 6),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: bordeTarjeta),
        ),
      ),
      // Campos de texto con fondo blanco y esquinas redondeadas
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
      ),
      // Botones con esquinas redondeadas
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      ),
    );
  }
}
