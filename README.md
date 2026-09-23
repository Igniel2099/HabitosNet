# HabitosNet

Aplicación .NET 10 MAUI para organizar proyectos y tareas en Windows y Android. Los datos se guardan en SQLite en el dispositivo; no se sincronizan entre el ordenador y el móvil.

## Uso

- **Inicio:** tareas pendientes, filtro de completadas, contadores y resumen por categoría. En una pantalla pequeña, el resumen se despliega con un botón.
- **Proyectos:** búsqueda por nombre/descripción, progreso y acceso al detalle.
- **Categorías y etiquetas:** nombres, colores y organización de proyectos. Una categoría utilizada debe reasignarse antes de eliminarla.
- Al añadir tareas a un proyecto nuevo, pulsa **Guardar proyecto** para conservar el conjunto. Las tareas se pueden editar, completar y eliminar mientras son borradores.
- Borrar un proyecto elimina sus tareas y asociaciones con etiquetas. Las etiquetas compartidas se conservan. La operación pide confirmación.
- La apariencia clara/oscura se conserva entre sesiones.

## Distribución adaptable

La interfaz responde al ancho real, incluido el cambio de tamaño de una ventana y la orientación de Android. Los formularios se limitan a 920 unidades y pasan a dos columnas a partir de 800. Los proyectos usan una, dos o tres columnas; la navegación lateral se mantiene abierta en ventanas amplias. Las listas principales usan CollectionView y las acciones táctiles tienen un mínimo de 48 unidades.

## Compilar

Requiere .NET 10 y las cargas de trabajo MAUI correspondientes. Android también necesita su SDK y JDK; Visual Studio puede instalarlos.

```powershell
dotnet build HabitosNet/HabitosNet.csproj -f net10.0-windows10.0.19041.0
dotnet build HabitosNet/HabitosNet.csproj -f net10.0-android
```

Para generar un APK de pruebas que incluya los ensamblados de la aplicación (sin depender del despliegue rápido de Visual Studio):

```powershell
dotnet build HabitosNet/HabitosNet.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true
```

El resultado firmado para desarrollo queda en `HabitosNet/bin/Debug/net10.0-android/com.companyname.habitosnet-Signed.apk`.

Abre `HabitosNet.slnx` en Visual Studio para ejecutar en Windows o un dispositivo/emulador Android. El primer build Android crea una clave de firma de desarrollo en el perfil de usuario.

## Regresiones

```powershell
dotnet run --project HabitosNet.Tests/HabitosNet.Tests.csproj
```

El ejecutable de pruebas usa los repositorios y PageModels reales contra bases SQLite temporales, y termina con código distinto de cero si hay fallos. Las APIs de interfaz se sustituyen por dobles para verificar navegación, avisos y confirmaciones. No modifica la base de datos de la aplicación. Detalles en [HabitosNet.Tests/README.md](HabitosNet.Tests/README.md).

Se comprueban borrado y reversión de proyectos, categorías utilizadas, tareas de borradores, validaciones, fallos SQL, reintentos, orden guardado/navegación e importación del JSON de ejemplo.

## Comprobación visual en dispositivo

La compilación y las pruebas de lógica no sustituyen esta revisión:

1. Windows: redimensionar entre 360, 800 y 1280 unidades; recorrer los controles con Tab y comprobar nombres/descripciones largos.
2. Android: comprobar vertical y horizontal, teclado visible al editar y tamaño de fuente ampliado.
3. En ambos: tema claro/oscuro; añadir proyecto con tareas, completar un borrador, guardarlo y reabrirlo; intentar guardar una tarea sin proyecto y borrar una categoría utilizada.

La dependencia SQLite conserva por ahora la versión original y sus avisos NU1903. Las opciones de actualización están en [docs/sqlite-security.md](docs/sqlite-security.md).
