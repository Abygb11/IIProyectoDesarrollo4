---------------------------------------------------------------------------------------------------
                               UNIVERSIDAD DE COSTA RICA
                        Sede de Occidente, Recinto de San Ramón
                              Desarrollo de Software IV

                                     Integrantes
                           Sebastián Alvarado García C5C341
                         Justin Andrés Badilla Ramírez C4C928
                         Abigail Crystal García Bonilla C5F263
                       Frank de Jesús Villalobos Elizondo C5K944

                                    II Ciclo, 2026.
---------------------------------------------------------------------------------------------------

-------Tecnología de la aplicación web-------
Elegimos la Opción A: .NET MVC porque nos permite trabajar todo en C#, y mantener la API limitada a lo que necesita la app móvil. De esta manera, usando todo en una sola compilación y por unicidad entre las direntes áreas compuestas por el proyecto.


-------Repositorio en Git-------
https://github.com/Abygb11/IIProyectoDesarrollo4.git



-------Datos de prueba-------
admin@vet.com → ves Administración y Veterinaria.
laura@vet.com → solo Veterinaria.
maria@correo.com → ninguno de los dos.

Clave123* para todos


-------Somee------
instancia: VeterinariaWeb.mssql.somee.com 
user: segundoproyecto_SQLLogin_1 
contraseña: 2doProyecto
BD: VeterinariaWeb


-------Aplicación móvil-------
Tecnología: Flutter (Dart), para Android. Está en la carpeta veterinaria_movil/ del repositorio.
Es de solo consulta para el perfil Dueño: inicio de sesión (envía el token JWT en cada petición),
Mis mascotas, vacunas (las vencidas en rojo) y resultados de exámenes con descarga del PDF o la imagen.

Cómo ejecutarla
cd veterinaria_movil
flutter pub get
flutter run
No se prueba en Chrome ni en Windows: la API no tiene CORS.

Dónde se cambia la URL de la API: veterinaria_movil/lib/config.dart
const String baseUrl = 'http://veterinariaweb.somee.com';
Es http porque Somee no tiene SSL activo; por eso el AndroidManifest.xml tiene android:usesCleartextTraffic="true".

Usuario de prueba (dueño): maria@correo.com / Clave123* → Rocky y Luna.
admin@vet.com y bloqueado@correo.com no pueden entrar a la app (la API responde 403).

APK:
cd veterinaria_movil
flutter build apk --release
Queda en veterinaria_movil/build/app/outputs/flutter-apk/app-release.apk 