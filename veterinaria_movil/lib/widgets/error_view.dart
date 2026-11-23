import 'package:material_ui/material_ui.dart';

import '../services/errores.dart';

// Texto para mostrarle al usuario según el tipo de error.
String mensajeDeError(Object error) {
  if (error is SinConexionException) {
    return 'Sin conexión. Revisa tu internet e intenta de nuevo.';
  }
  if (error is SesionExpiradaException) {
    return 'Tu sesión expiró. Inicia sesión de nuevo.';
  }
  if (error is ApiException) {
    return error.mensaje;
  }
  return 'Ocurrió un error inesperado. Intenta de nuevo.';
}

// Sesión expirada: avisa con un SnackBar y vuelve al login borrando las pantallas anteriores,
// para que con el botón "atrás" no se pueda volver a una pantalla sin sesión.
void irAlLogin(BuildContext context) {
  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(content: Text('Tu sesión expiró. Inicia sesión de nuevo.')),
  );
  Navigator.of(context).pushNamedAndRemoveUntil('/login', (ruta) => false);
}

// Lo que se muestra en lugar de una lista cuando la carga falla.
class ErrorView extends StatefulWidget {
  final Object error;
  final VoidCallback onReintentar; // función que vuelve a pedir los datos

  const ErrorView({super.key, required this.error, required this.onReintentar});

  @override
  State<ErrorView> createState() => _ErrorViewState();
}

class _ErrorViewState extends State<ErrorView> {
  @override
  void initState() {
    super.initState();
    if (widget.error is SesionExpiradaException) {
      // No se puede cambiar de pantalla mientras esta se está dibujando, así que se espera a que termine de dibujarse y ahí se va al login.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) irAlLogin(context);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final sinConexion = widget.error is SinConexionException;
    final sesionExpirada = widget.error is SesionExpiradaException;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              sinConexion ? Icons.wifi_off : Icons.error_outline,
              size: 64,
              color: Colors.grey,
            ),
            const SizedBox(height: 16),
            Text(
              mensajeDeError(widget.error),
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 16),
            ),
            const SizedBox(height: 16),
            //  la app ya se va al login.
            if (!sesionExpirada)
              FilledButton.icon(
                onPressed: widget.onReintentar,
                icon: const Icon(Icons.refresh),
                label: const Text('Reintentar'),
              ),
          ],
        ),
      ),
    );
  }
}
