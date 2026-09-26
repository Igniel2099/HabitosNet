# 🤖 Agent Directives — HabitosNet (.NET 10 MAUI App)

## 📌 Contexto del Proyecto
**HabitosNet** es una aplicación multiplataforma (Android y Windows Desktop) desarrollada en **.NET 10 MAUI** para el seguimiento y registro diario de hábitos. 

## 🛠️ Tech Stack & Arquitectura
* **Framework:** .NET 10 MAUI
* **Plataformas:** Android y Windows (`net10.0-android`, `net10.0-windows10.0.19041.0`)
* **Patrón de Arquitectura:** MVVM (Model-View-ViewModel) estricto.
* **Persistencia:** Entity Framework Core 10 con SQLite (`Microsoft.EntityFrameworkCore.Sqlite`).
* **Inyección de Dependencias:** `Microsoft.Extensions.DependencyInjection` mediante `IDbContextFactory<AppDbContext>` para contextos efímeros en llamadas asíncronas.
* **Navegación:** `AppShell` con pestañas inferiores (`TabBar`).

## ⚙️ Reglas de Código y Principios
1. **Model-View-ViewModel (MVVM):**
   * Toda la lógica de presentación debe residir en los **ViewModels**.
   * Las vistas XAML no deben contener código en el *Code-Behind* (`.xaml.cs`) salvo la inicialización del componente o asignación básica del ViewModel.
   * Usar data binding bidireccional (`Mode=TwoWay`) o comandos (`ICommand` / `RelayCommand`) para la interacción con la UI.

2. **Entity Framework Core & Concurrencia:**
   * Utilizar siempre `IDbContextFactory<AppDbContext>` para instanciar contextos cortos (`using var context = await _contextFactory.CreateDbContextAsync()`) dentro de métodos asíncronos del ViewModel.
   * Mantener las entidades livianas y bien tipadas.

3. **Restricciones de Negocio:**
   * **Edición diaria:** Solo se puede editar el registro correspondiente al día de HOY (`DateTime.Today`). Días pasados o futuros son de solo lectura.
    * **Sin registros inventados:** Si un día pasado no tiene registro, se crea como NO completado (todo en `false`/`null`, solo se heredan los objetivos). Si al final del día no registraste nada, nada queda completado.
   * **Fallback de Placeholders:** Si el usuario no ha ingresado un tiempo real en tareas medibles (Leer, Meditar, Estudiar), se usará como valor/placeholder el tiempo ingresado el día anterior.

4. **Diseño UI / UX:**
   * Utilizar contenedores redondeados tipo `Border` para lograr estética de tarjetas (*Cards*).
   * Mantener soporte limpio para Tema Claro y Oscuro (`AppThemeBinding`).
   * Optimizar la distribución tanto para pantalla táctil en Android como para ventana en Windows.

## 📋 Responsabilidades del Agente
* Asistir en la implementación de las 3 pantallas principales (Home, Estadísticas, Ajustes).
* Generar código limpio, documentado, con tipado fuerte y siguiendo las convenciones oficiales de C# y .NET 10.
* Recomendar activamente mejoras de rendimiento, refactorizaciones y patrones de diseño accesibles y mantenibles.