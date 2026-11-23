import 'package:material_ui/material_ui.dart';

import '../estilos/estilos.dart';
import '../models/mascota.dart';
import '../models/vacuna.dart';
import '../services/api_service.dart';
import '../widgets/error_view.dart';

// Vacunas de una mascota. Las vencidas se marcan en rojo.
class VacunasScreen extends StatefulWidget {
  final Mascota mascota;

  const VacunasScreen({super.key, required this.mascota});

  @override
  State<VacunasScreen> createState() => _VacunasScreenState();
}

class _VacunasScreenState extends State<VacunasScreen> {
  late Future<List<Vacuna>> _vacunas;

  @override
  void initState() {
    super.initState();
    _vacunas = ApiService.getVacunas(widget.mascota.id);
  }

  // Reintentar
  void _cargar() {
    setState(() {
      _vacunas = ApiService.getVacunas(widget.mascota.id);
    });
  }

  // Deslizar hacia abajo
  Future<void> _refrescar() async {
    final nuevas = ApiService.getVacunas(widget.mascota.id);
    try {
      await nuevas;
    } catch (e) {
      // el error lo muestra ErrorView
    }
    if (!mounted) return;
    setState(() {
      _vacunas = nuevas;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Vacunas de ${widget.mascota.nombre}')),
      body: FutureBuilder<List<Vacuna>>(
        future: _vacunas,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onReintentar: _cargar);
          }
          final vacunas = snapshot.data!;
          return RefreshIndicator(
            onRefresh: _refrescar,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              children: [
                if (vacunas.isEmpty) _vacio(),
                for (final vacuna in vacunas) _tarjeta(vacuna),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _tarjeta(Vacuna vacuna) {
    // vencida la calcula el servidor
    final color = vacuna.vencida ? Estilos.vencida : Estilos.alDia;

    return Card(
      // mismo borde redondeado del tema, pero con el color de la vacuna
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: color, width: 1.5),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    vacuna.nombre,
                    style: Estilos.titulo,
                  ),
                ),
                _etiqueta(vacuna.vencida ? 'VENCIDA' : 'Al día', color),
              ],
            ),
            const SizedBox(height: 8),
            Text('Aplicada: ${Estilos.formatoFecha.format(vacuna.fechaAplicacion)}'),
            Text(
              'Próxima dosis: ${Estilos.formatoFecha.format(vacuna.fechaProximaDosis)}',
              style: TextStyle(
                color: vacuna.vencida ? color : null,
                fontWeight: vacuna.vencida ? FontWeight.bold : null,
              ),
            ),
            Text(
              'Aplicada por: ${vacuna.autor}',
              style: Estilos.secundario,
            ),
          ],
        ),
      ),
    );
  }

  // Etiqueta de color: VENCIDA (rojo) o Al dia (verde).
  Widget _etiqueta(String texto, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(12)),
      child: Text(
        texto,
        style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12),
      ),
    );
  }

  // Sin vacunas registradas.
  Widget _vacio() {
    return const Padding(
      padding: EdgeInsets.only(top: 80),
      child: Column(
        children: [
          Icon(Icons.vaccines, size: 64, color: Estilos.gris),
          SizedBox(height: 16),
          Text('Esta mascota todavía no tiene vacunas registradas.', textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
