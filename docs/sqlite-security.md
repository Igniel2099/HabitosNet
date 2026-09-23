# Opciones para la alerta de SQLite

Revisión de fuentes: 23 de septiembre de 2026. Estas opciones se documentan; no se han aplicado cambios de paquetes.

La aplicación utiliza Microsoft.Data.Sqlite.Core 8.0.8 y SQLitePCLRaw.bundle_green 2.1.10. El aviso NU1903 identifica bibliotecas nativas transitivas `SQLitePCLRaw.lib.e_sqlite3` y `SQLitePCLRaw.lib.e_sqlite3.android`.

[CVE-2025-6965 / GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q) afecta a SQLite anterior a 3.50.2 y describe corrupción de memoria al procesar determinadas consultas. La ficha enumera las versiones de esos paquetes hasta 2.1.11 como afectadas. La aplicación utiliza consultas fijas y parametrizadas: el aviso no demuestra por sí mismo una vía de explotación desde sus formularios.

## Opción recomendada: migrar al bundle actual

Sustituir `SQLitePCLRaw.bundle_green` por `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5, revisando las dependencias transitivas junto a Microsoft.Data.Sqlite.Core. El paquete publicado incluye dependencia de SQLite nativo >=3.53.4 y configuración e_sqlite3 >=3.0.5. Mantiene SQLite como almacenamiento; no requiere convertir los datos a otro motor.

Es la opción preferida para continuar el mantenimiento. Requiere comprobar inicialización, restauración de paquetes, arquitectura Windows x64/ARM64 según destino y Android arm64/x64, además de las operaciones de base de datos. Si posteriormente se distribuye iOS, cambia el uso de SQLite del sistema por la biblioteca incluida.

Fuentes: [paquete 3.0.5 y dependencias](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/3.0.5), [notas de migración 3.0](https://github.com/ericsink/SQLitePCL.raw/blob/main/v3.md).

## Opción conservadora: mantener la serie 2.x

Sustituir `bundle_green` por `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13. Sus dependencias Windows y Android exigen las bibliotecas nativas >=2.1.13, fuera del intervalo afectado que figura en el aviso consultado. Esta vía reduce el cambio de versión mayor, aunque mantiene la aplicación en la serie anterior.

No basta con subir `bundle_green` a 2.1.11: sigue incluyendo versiones afectadas. Su paquete público sigue en 2.1.11 y fue retirado de la serie 3.x.

Fuentes: [bundle_e_sqlite3 2.1.13](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.13), [bundle_green](https://www.nuget.org/packages/SQLitePCLRaw.bundle_green), [biblioteca nativa Android 2.1.13](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3.android/2.1.13).

## Validación de cualquiera de las opciones

1. Actualizar también las referencias equivalentes del ejecutable de regresiones.
2. Restaurar y auditar dependencias transitivas (`dotnet list HabitosNet/HabitosNet.csproj package --vulnerable --include-transitive`). Comprobar que ya no se arrastran los paquetes nativos antiguos.
3. Ejecutar las regresiones y compilar Windows y Android.
4. Verificar en ambos destinos `SELECT sqlite_version()`; el motor cargado debe ser al menos 3.50.2 para este CVE y estar libre de otros avisos conocidos aplicables.
5. Abrir una copia de una base existente y verificar altas, ediciones, borrados y conservación de datos. No es necesario restablecer los datos de usuario.

Ocultar NU1903 no corrige el motor que ejecuta la aplicación. Tampoco basta con actualizar solo Microsoft.Data.Sqlite.Core mientras se conserve el bundle nativo vulnerable.
