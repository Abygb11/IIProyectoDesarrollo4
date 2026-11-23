import 'package:material_ui/material_ui.dart';

import '../estilos/estilos.dart';
import '../models/mascota.dart';
import '../services/api_service.dart';
import '../services/sesion_service.dart';
import '../widgets/error_view.dart';
import '../widgets/foto_mascota.dart';
import 'menu_mascota_screen.dart';

// Pantalla "Mis mascotas".
class MascotasScreen extends StatefulWidget {
  const MascotasScreen({super.key});

  @override
  State<MascotasScreen> createState() => _MascotasScreenState();
}

class _MascotasScreenState extends State<MascotasScreen> {
  late Future<List<Mascota>> _mascotas;

  @override
  void initState() {
    super.initState();
    _mascotas = ApiService.getMascotas(); // se pide una sola vez
  }

  // Reintentar
  void _cargar() {
    setState(() {
      _mascotas = ApiService.getMascotas();
    });
  }

  // Deslizar hacia abajo: la lista vieja se ve hasta que llega la nueva.
  Future<void> _refrescar() async {
    final nueva = ApiService.getMascotas();
    try {
      await nueva;
    } catch (e) {
      // el error lo muestra ErrorView
    }
    if (!mounted) return;
    setState(() {
      _mascotas = nueva;
    });
  }

  // push (no replace): con "atras" se vuelve a la lista.
  void _abrirMenu(Mascota mascota) {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => MenuMascotaScreen(mascota: mascota)),
    );
  }

  Future<void> _cerrarSesion() async {
    final confirmar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cerrar sesión'),
        content: const Text('¿Seguro que quieres salir?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Salir')),
        ],
      ),
    );
    if (confirmar != true) return;

    await SesionService.borrarSesion();
    if (!mounted) return;
    Navigator.of(context).pushNamedAndRemoveUntil('/login', (ruta) => false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Hola, ${SesionService.nombre ?? ''}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Cerrar sesión',
            onPressed: _cerrarSesion,
          ),
        ],
      ),
      body: FutureBuilder<List<Mascota>>(
        future: _mascotas,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onReintentar: _cargar);
          }
          final mascotas = snapshot.data!;
          return RefreshIndicator(
            onRefresh: _refrescar,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(), // permite refrescar con pocas mascotas
              padding: const EdgeInsets.all(16),
              children: [
                Text('Mis mascotas', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                if (mascotas.isEmpty) _vacio(),
                for (final mascota in mascotas) _tarjeta(mascota),
              ],
            ),
          );
        },
      ),
    );
  }

  // Foto, nombre y "especie · raza".
  Widget _tarjeta(Mascota mascota) {
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        leading: FotoMascota(mascota: mascota, tamano: 52),
        title: Text(mascota.nombre, style: Estilos.titulo),
        subtitle: Text('${mascota.especie} · ${mascota.raza}'),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => _abrirMenu(mascota),
      ),
    );
  }

  // Sin mascotas registradas.
  Widget _vacio() {
    return const Padding(
      padding: EdgeInsets.only(top: 80),
      child: Column(
        children: [
          Icon(Estilos.iconoAnimal, size: 64, color: Estilos.gris),
          SizedBox(height: 16),
          Text(
            'Todavía no tienes mascotas registradas.\nPídele a la clínica que las registre.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
