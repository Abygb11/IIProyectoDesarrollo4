import 'package:material_ui/material_ui.dart';

import '../estilos/estilos.dart';
import '../models/examen.dart';
import '../models/mascota.dart';
import '../services/api_service.dart';
import '../services/errores.dart';
import '../widgets/error_view.dart';

// Resultados de examenes. Al tocar uno se descarga y se abre.
class ExamenesScreen extends StatefulWidget {
  final Mascota mascota;

  const ExamenesScreen({super.key, required this.mascota});

  @override
  State<ExamenesScreen> createState() => _ExamenesScreenState();
}

class _ExamenesScreenState extends State<ExamenesScreen> {
  late Future<List<Examen>> _examenes;
  int? _descargandoId; // id del examen que se esta descargando (null = ninguno)

  @override
  void initState() {
    super.initState();
    _examenes = ApiService.getExamenes(widget.mascota.id);
  }

  // Reintentar
  void _cargar() {
    setState(() {
      _examenes = ApiService.getExamenes(widget.mascota.id);
    });
  }

  // Deslizar hacia abajo
  Future<void> _refrescar() async {
    final nuevos = ApiService.getExamenes(widget.mascota.id);
    try {
      await nuevos;
    } catch (e) {
      // el error lo muestra ErrorView
    }
    if (!mounted) return;
    setState(() {
      _examenes = nuevos;
    });
  }

  // Descarga y abre el archivo. Los errores salen en un SnackBar, la lista se queda.
  Future<void> _abrir(Examen examen) async {
    if (_descargandoId != null) return; // uno a la vez
    setState(() => _descargandoId = examen.id);

    try {
      await ApiService.descargarExamen(widget.mascota.id, examen);
    } catch (e) {
      if (!mounted) return;
      if (e is SesionExpiradaException) {
        irAlLogin(context);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(mensajeDeError(e))),
        );
      }
    }

    if (!mounted) return;
    setState(() => _descargandoId = null);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Exámenes de ${widget.mascota.nombre}')),
      body: FutureBuilder<List<Examen>>(
        future: _examenes,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return ErrorView(error: snapshot.error!, onReintentar: _cargar);
          }
          final examenes = snapshot.data!;
          return RefreshIndicator(
            onRefresh: _refrescar,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              children: [
                if (examenes.isNotEmpty)
                  const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: Text('Toca un examen para descargarlo y verlo.'),
                  ),
                if (examenes.isEmpty) _vacio(),
                for (final examen in examenes) _tarjeta(examen),
              ],
            ),
          );
        },
      ),
    );
  }

  // Icono PDF o imagen, descripcion, fecha y autor.
  Widget _tarjeta(Examen examen) {
    final descargando = _descargandoId == examen.id;

    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        leading: Icon(
          examen.esPdf ? Icons.picture_as_pdf : Icons.image,
          size: 36,
          color: examen.esPdf ? Estilos.pdf : Estilos.imagen,
        ),
        title: Text(examen.descripcion, style: Estilos.negrita),
        subtitle: Text(
          'Fecha: ${Estilos.formatoFecha.format(examen.fecha)}\nRegistrado por: ${examen.autor}',
        ),
        isThreeLine: true,
        // spinner solo en la tarjeta que se descarga
        trailing: descargando
            ? const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(strokeWidth: 2))
            : const Icon(Icons.download),
        onTap: () => _abrir(examen),
      ),
    );
  }

  // Sin examenes registrados.
  Widget _vacio() {
    return const Padding(
      padding: EdgeInsets.only(top: 80),
      child: Column(
        children: [
          Icon(Icons.description, size: 64, color: Estilos.gris),
          SizedBox(height: 16),
          Text('Esta mascota todavía no tiene exámenes registrados.', textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
