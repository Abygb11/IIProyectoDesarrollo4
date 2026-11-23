import 'package:material_ui/material_ui.dart';

import 'estilos/estilos.dart';
import 'screens/login_screen.dart';
import 'screens/mascotas_screen.dart';
import 'services/sesion_service.dart';

Future<void> main() async {
  // Necesario para poder leer flutter_secure_storage antes de runApp.
  WidgetsFlutterBinding.ensureInitialized();

  // Pasa a memoria la sesión que quedó guardada en el celular (si hay una).
  // Si el almacenamiento falla, se borra y la app arranca en el login en vez de cerrarse.
  try {
    await SesionService.leerSesion();
  } catch (e) {
    await SesionService.borrarSesion();
  }

  runApp(const VeterinariaApp());
}

class VeterinariaApp extends StatelessWidget {
  const VeterinariaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Veterinaria',
      debugShowCheckedModeBanner: false, // quita la etiqueta "DEBUG" de la esquina
      theme: Estilos.tema, // lib/estilos/estilos.dart

      // Si hay una sesión que no ha vencido, entra directo a Mis mascotas; si no, al login.
      initialRoute: SesionService.haySesionValida ? '/mascotas' : '/login',

      // Pantallas a las que se llega por nombre (error_view.dart usa '/login').
      routes: {
        '/login': (context) => const LoginScreen(),
        '/mascotas': (context) => const MascotasScreen(),
      },
    );
  }
}
