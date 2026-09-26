# HabitosNet

Aplicación .NET 10 MAUI para el seguimiento diario de hábitos, en Android y Windows. Los datos se guardan en SQLite en el dispositivo; no se sincronizan entre el ordenador y el móvil.

## Pantallas

- **Home:** registro diario. Selector semanal (Lun–Dom, formato `LUN 28`), tarjetas por hábito (Despertar con `TimePicker`; Leer, Meditar y Estudiar con objetivo, campo numérico + `Stepper` y `CheckBox`). Solo el día de hoy es editable; el resto es de solo lectura. Al completar, el texto se tacha.
- **Estudiar:** temporizador Pomodoro (Trabajo → Corto ×3 → Largo, ciclo de 4 con encadenado automático). Tiempos configurables guardados en `Preferences`. Al minimizar, congela y al volver recalcula por diferencia (sin servicios en segundo plano). Cada pomodoro completado suma minutos al registro de hoy.
- **Histórico:** una semana visible con acordeón por día (`Expander`): cabecera con `X/5 tareas` y detalle de tiempos. Navegación entre semanas con ◀ ▶. El día actual (o el último con datos) abre expandido por defecto.

## Reglas de negocio

- **Edición diaria:** solo se edita el registro de HOY. Días pasados o futuros son de solo lectura.
- **Sin registros inventados:** un día pasado sin registro se crea como NO completado (todo en `false`/`null`, solo se heredan los objetivos). Si al final del día no registraste nada, nada queda completado.
- **Placeholders:** si el campo de tiempo está vacío, el placeholder muestra el tiempo real del día anterior; al marcar completado sin escribir nada, se asigna ese valor.

## Arquitectura

- MVVM estricto: lógica en `ViewModels`, XAML sin code-behind salvo inyección del ViewModel.
- Entity Framework Core 10 + SQLite con `IDbContextFactory<AppDbContext>` (contextos efímeros).
- Entidad central `DailyRegister` (un registro por fecha, índice único).
- `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`) y `CommunityToolkit.Maui` (`Expander`, `UseMauiCommunityToolkit`).

## Compilar

Requiere .NET 10 y las cargas de trabajo MAUI. Android además necesita su SDK y JDK; Visual Studio puede instalarlos.

```powershell
dotnet build HabitosNet.slnx -v minimal
```

Por plataformas:

```powershell
dotnet build HabitosNet/HabitosNet.csproj -f net10.0-windows10.0.19041.0
dotnet build HabitosNet/HabitosNet.csproj -f net10.0-android
```

Abre `HabitosNet.slnx` en Visual Studio para ejecutar en Windows o un dispositivo/emulador Android.

## Notas técnicas

- `Microsoft.Maui.Controls` está fijado a `10.0.90` porque `CommunityToolkit.Maui 15.0.1` lo exige (sin el pin falla el restore con NU1605).
- El Histórico usa botones ◀ ▶ en lugar de `CarouselView`: el carrusel vía pestañas de Shell provoca un crash nativo solo en Windows.
- Quedan avisos MVVMTK0045 (compatibilidad AOT en WinUI por usar `[ObservableProperty]` con fields); no bloquean la compilación.
- Para empezar con datos limpios durante el desarrollo, desinstala la app o borra sus datos (la BD vive en `FileSystem.AppDataDirectory/habitos.db`).
