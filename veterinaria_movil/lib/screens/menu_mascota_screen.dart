import 'package:material_ui/material_ui.dart';

import '../estilos/estilos.dart';
import '../models/mascota.dart';
import '../widgets/foto_mascota.dart';
import 'examenes_screen.dart';
import 'vacunas_screen.dart';

// Menu de una mascota: foto grande, datos y las dos opciones.
class MenuMascotaScreen extends StatelessWidget {
  final Mascota mascota;

  const MenuMascotaScreen({super.key, required this.mascota});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(mascota.nombre)),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Center(child: FotoMascota(mascota: mascota, tamano: 140)),
          const SizedBox(height: 16),
          Text(
            mascota.nombre,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          Text(
            '${mascota.especie} · ${mascota.raza}',
            textAlign: TextAlign.center,
            style: Estilos.secundario.copyWith(fontSize: 16),
          ),
          const SizedBox(height: 32),
          _opcion(
            icono: Icons.vaccines,
            titulo: 'Ver vacunas',
            detalle: 'Aplicadas y fecha de la próxima dosis',
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (context) => VacunasScreen(mascota: mascota)),
            ),
          ),
          _opcion(
            icono: Icons.description,
            titulo: 'Ver resultados de exámenes',
            detalle: 'Descarga y abre el PDF o la imagen',
            onTap: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (context) => ExamenesScreen(mascota: mascota)),
            ),
          ),
        ],
      ),
    );
  }

  // Tarjeta de una opcion del menu.
  Widget _opcion({
    required IconData icono,
    required String titulo,
    required String detalle,
    required VoidCallback onTap,
  }) {
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        // Icono dentro de un cuadrito rosado
        leading: Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: Estilos.fondoIcono,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icono, size: 28, color: Estilos.primario),
        ),
        title: Text(titulo, style: Estilos.negrita),
        subtitle: Text(detalle),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
