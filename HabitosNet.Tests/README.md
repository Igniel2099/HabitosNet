# Pruebas de regresión

Ejecutar desde la raíz del repositorio con el SDK .NET 10:

```powershell
dotnet run --project HabitosNet.Tests/HabitosNet.Tests.csproj
```

Es un ejecutable de pruebas que devuelve un código distinto de cero si falla alguna
comprobación. Usa los repositorios, modelos y PageModels reales, enlazados desde la aplicación,
y una base SQLite temporal distinta por caso. Nunca utiliza la base de la aplicación.
No requiere instalar MAUI ni un emulador; `MauiTestDoubles.cs` sustituye únicamente
las APIs visuales, la navegación y el acceso a archivos del paquete. Los comandos
se generan con CommunityToolkit.Mvvm, igual que en la aplicación.

Cubre el borrado y rollback de proyectos, bases creadas con el esquema anterior,
la protección de categorías usadas, las referencias a proyectos/categorías ausentes,
y la validación de los datos de ejemplo antes de restablecer. También recorre la
creación y finalización de tareas dentro de un proyecto aún sin guardar, el intento
de guardar sin proyecto, la preservación del formulario ante un fallo SQL, el
reintento, la persistencia antes de navegar y la categoría usada que debe permanecer
en pantalla. Los fallos SQL se provocan con triggers dentro de bases temporales.

Estas pruebas no verifican el dibujo de XAML, los tamaños de pantalla ni los servicios
nativos de Windows/Android, que necesitan comprobación adicional en la aplicación.

Las versiones de SQLite
son las mismas que en la aplicación, incluido el aviso de seguridad pendiente de
evaluar; no se oculta en la configuración del proyecto.
