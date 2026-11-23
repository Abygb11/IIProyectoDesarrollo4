import 'package:material_ui/material_ui.dart';

import '../estilos/estilos.dart';
import '../services/api_service.dart';
import '../widgets/error_view.dart';

// Pantalla de inicio de sesión del dueño.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>(); // para validar los dos campos juntos
  final _correoController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _ocultarPassword = true;
  bool _cargando = false; 
  String? _error;

  @override
  void dispose() {
    // Los controladores se liberan al cerrar la pantalla.
    _correoController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _iniciarSesion() async {
    if (_cargando) return; // evita mandar el login dos veces
    if (!_formKey.currentState!.validate()) return; // campos vacíos o correo inválido

    FocusScope.of(context).unfocus(); // esconde el teclado
    setState(() {
      _cargando = true;
      _error = null;
    });

    try {
      await ApiService.login(_correoController.text.trim(), _passwordController.text);
      if (!mounted) return; // la pantalla pudo cerrarse mientras se esperaba
      Navigator.of(context).pushReplacementNamed('/mascotas');  // pushReplacement: el login se quita, así "atrás" no vuelve a esta pantalla.
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = mensajeDeError(e); // "Correo o contraseña incorrectos.", "Sin conexión...", etc.
        _cargando = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;

    return Scaffold(
      body: SafeArea(
        child: Center(
          // SingleChildScrollView: cuando sale el teclado se puede deslizar y nada se tapa.
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo: conejo blanco sobre un cuadro rojo vino
                  Center(
                    child: Container(
                      width: 110,
                      height: 110,
                      decoration: BoxDecoration(
                        color: Estilos.primario,
                        borderRadius: BorderRadius.circular(32),
                      ),
                      child: const Icon(Estilos.iconoAnimal, size: 64, color: Colors.white),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Veterinaria',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineMedium,
                  ),
                  const Text(
                    'Consulta el expediente de tus mascotas',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 32),

                  // Correo
                  TextFormField(
                    controller: _correoController,
                    keyboardType: TextInputType.emailAddress, // teclado con @
                    textInputAction: TextInputAction.next, // "siguiente" pasa a la contraseña
                    enabled: !_cargando,
                    decoration: const InputDecoration(
                      labelText: 'Correo',
                      prefixIcon: Icon(Icons.email_outlined),
                    ),
                    validator: (valor) {
                      if (valor == null || valor.trim().isEmpty) return 'Escribe tu correo.';
                      if (!valor.contains('@')) return 'El correo no es válido.';
                      return null; // null = el campo está bien
                    },
                  ),
                  const SizedBox(height: 16),

                  // Contraseña con el botón del ojo
                  TextFormField(
                    controller: _passwordController,
                    obscureText: _ocultarPassword,
                    textInputAction: TextInputAction.done,
                    enabled: !_cargando,
                    onFieldSubmitted: (_) => _iniciarSesion(), // "listo" en el teclado = entrar
                    decoration: InputDecoration(
                      labelText: 'Contraseña',
                      prefixIcon: const Icon(Icons.lock_outline),
                      suffixIcon: IconButton(
                        icon: Icon(_ocultarPassword ? Icons.visibility : Icons.visibility_off),
                        tooltip: _ocultarPassword ? 'Mostrar contraseña' : 'Ocultar contraseña',
                        onPressed: () => setState(() => _ocultarPassword = !_ocultarPassword),
                      ),
                    ),
                    validator: (valor) {
                      if (valor == null || valor.isEmpty) return 'Escribe tu contraseña.';
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),

                  // Error de la API o de red, en rojo
                  if (_error != null)
                    Container(
                      padding: const EdgeInsets.all(12),
                      margin: const EdgeInsets.only(bottom: 16),
                      decoration: BoxDecoration(
                        color: colores.errorContainer,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.error_outline, color: colores.error),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(_error!, style: TextStyle(color: colores.onErrorContainer)),
                          ),
                        ],
                      ),
                    ),

                  // Botón: deshabilitado (onPressed null) y con spinner mientras carga
                  FilledButton(
                    onPressed: _cargando ? null : _iniciarSesion,
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    child: _cargando
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Iniciar sesión'),
                  ),
                  const SizedBox(height: 24),

                  const Text(
                    '¿No tienes cuenta? Regístrate en la página web de la clínica.',
                    textAlign: TextAlign.center,
                    style: Estilos.secundario,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
