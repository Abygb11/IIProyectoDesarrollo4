import 'package:material_ui/material_ui.dart';

import '../config.dart';
import '../estilos/estilos.dart';
import '../models/mascota.dart';
import '../services/api_service.dart';

// Si la mascota no tiene foto, o la foto no carga, muestra el ícono de una huella.
class FotoMascota extends StatelessWidget {
  final Mascota mascota;
  final double tamano;

  const FotoMascota({super.key, required this.mascota, this.tamano = 56});

  @override
  Widget build(BuildContext context) {
    // Cuadro con esquinas redondeadas (no circulo)
    return ClipRRect(
      borderRadius: BorderRadius.circular(tamano * 0.3),
      child: SizedBox(
        width: tamano,
        height: tamano,
        // Si tieneFoto es false no se llama a la API, se pone el ícono directo.
        child: mascota.tieneFoto
            ? Image.network(
                '$baseUrl/api/mascotas/${mascota.id}/foto',
                headers: ApiService.encabezados, // la foto también pide el token
                fit: BoxFit.cover,
                // Sin internet, token vencido o 404: se muestra el ícono en vez de un error.
                errorBuilder: (context, error, stackTrace) => _icono(),
              ) 
            : _icono(),
      ),
    );
  }

  // Ícono que reemplaza la foto.
  Widget _icono() {
    return Container(
      color: Estilos.fondoIcono,
      child: Icon(Estilos.iconoAnimal, size: tamano * 0.5, color: Estilos.primario),
    );
  }
}
