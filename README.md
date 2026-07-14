# FerreSys

Sistema de punto de venta (POS) para ferretería. Aplicación de escritorio **Windows Forms** (.NET 10) con base de datos **SQLite** local.

## Requisitos

| Uso | Requisito |
|-----|-----------|
| Desarrollo / correr en local | [SDK de .NET 10](https://dotnet.microsoft.com/download) y Windows 10/11 |
| Máquina cliente (paquete self-contained) | Windows 10/11 **64-bit** — **no** necesita instalar .NET |
| Máquina cliente (paquete framework-dependent) | Windows 10/11 + [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |

Comprueba el SDK:

```powershell
dotnet --version
```

Debe ser `10.x` o superior.

## Estructura del proyecto

```
ferresys/
├── FerreSys.Domain/                      # Entidades e interfaces
├── FerreSys.Application/                 # Casos de uso
├── FerreSys.Infrastructure.Persistence/  # SQLite
├── FerreSys.Infrastructure.UI/           # WinForms (ejecutable)
├── Directory.Build.props                 # Versión de la aplicación
├── scripts/publish.ps1                   # Empaquetado
└── FerreSys.slnx
```

La base de datos `ferresys.db` se crea automáticamente **junto al ejecutable** la primera vez que se abre la app.

> **Importante:** no instales la app en `Program Files` si quieres guardar ventas/productos sin permisos de administrador. Usa una carpeta del usuario (recomendado: `%LOCALAPPDATA%\FerreSys` o una carpeta portable).

---

## Correr en local (desarrollo)

### 1. Clonar e ir al repo

```powershell
cd "C:\ruta\a\ferresys"
```

### 2. Restaurar y compilar

```powershell
dotnet restore FerreSys.slnx
dotnet build FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj -c Debug
```

### 3. Ejecutar

```powershell
dotnet run --project FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj
```

Desde Visual Studio / Cursor: abre `FerreSys.slnx`, selecciona el proyecto `FerreSys.Infrastructure.UI` y pulsa F5 (o usa la task `build` / `watch` de `.vscode/tasks.json`).

### Datos de desarrollo

Al ejecutar en Debug, la DB queda en:

```text
FerreSys.Infrastructure.UI\bin\Debug\net10.0-windows\ferresys.db
```

Ese archivo está en `.gitignore` (no se versiona). Si la borras, la app creará una DB vacía al reiniciar.

---

## Versionamiento

La versión única de FerreSys vive en [`Directory.Build.props`](Directory.Build.props) y se aplica a **todos** los proyectos al compilar.

```xml
<Version>0.1.0</Version>
<AssemblyVersion>0.1.0.0</AssemblyVersion>
<FileVersion>0.1.0.0</FileVersion>
<InformationalVersion>0.1.0</InformationalVersion>
```

Se muestra en el título de la ventana: `FerreSys MVP - Terminal de Mostrador v0.1.0`.

### Convención SemVer

| Parte | Cuándo subirla | Ejemplo |
|-------|----------------|---------|
| **MAJOR** (`1.0.0`) | Cambio incompatible (migración de DB obligatoria, romper flujos) | `0.1.0` → `1.0.0` |
| **MINOR** (`0.2.0`) | Funcionalidad nueva compatible | `0.1.0` → `0.2.0` |
| **PATCH** (`0.1.1`) | Corrección de bugs / ajustes menores | `0.1.0` → `0.1.1` |

### Checklist al liberar una versión

1. Actualiza `Version`, `AssemblyVersion`, `FileVersion` e `InformationalVersion` en `Directory.Build.props` (manténlos alineados).
2. Compila y prueba en local.
3. Empaqueta con `scripts\publish.ps1`.
4. Crea un tag git:

```powershell
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin v0.1.0
```

5. Sube el ZIP `artifacts\FerreSys-v0.1.0-win-x64.zip` a donde distribuyas (SharePoint, Drive, GitHub Releases, etc.).

---

## Empaquetar para distribución

### Opción recomendada (script)

Self-contained, un solo exe + dependencias nativas, listo para copiar a otras PCs **sin instalar .NET**:

```powershell
.\scripts\publish.ps1
```

Salida:

| Artefacto | Ubicación |
|-----------|-----------|
| Carpeta publicada | `artifacts\publish\win-x64\` |
| ZIP listo para enviar | `artifacts\FerreSys-v{versión}-win-x64.zip` |

El script **elimina** cualquier `ferresys.db` del paquete para no distribuir datos de desarrollo.

### Opción manual con `dotnet publish`

```powershell
dotnet publish FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o artifacts\publish\win-x64 `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

### Variante framework-dependent (más liviana)

La máquina destino **sí** necesita el Desktop Runtime de .NET 10:

```powershell
.\scripts\publish.ps1 -FrameworkDependent
```

### Parámetros del script

```powershell
.\scripts\publish.ps1 -Configuration Release -Runtime win-x64
.\scripts\publish.ps1 -FrameworkDependent
```

---

## Instalar en otras máquinas

1. Copia el ZIP `FerreSys-vX.Y.Z-win-x64.zip` a la PC destino.
2. Descomprímelo en una carpeta del usuario, por ejemplo:

   ```text
   C:\Users\<usuario>\AppData\Local\FerreSys\
   ```

   o

   ```text
   C:\FerreSys\
   ```

3. Ejecuta `FerreSys.Infrastructure.UI.exe`.
4. (Opcional) Crea un acceso directo en el Escritorio apuntando a ese `.exe`.
5. La primera ejecución crea `ferresys.db` en la **misma carpeta** del ejecutable.

### Antivirus / SmartScreen

En máquinas nuevas, Windows puede mostrar “Windows protegió tu PC” si el binario no está firmado. Usa “Más información → Ejecutar de todas formas”, o firma el ejecutable con un certificado de código si vas a distribuir a clientes.

### Qué no hacer

- No sobrescribas `ferresys.db` al actualizar (pierdes productos y ventas).
- No descomprimas “encima” a ciegas sin respaldar la DB primero.
- Evita `C:\Program Files\...` mientras la DB viva junto al `.exe` (permisos de escritura).

---

## Actualizaciones (cómo funcionan hoy)

Hoy el update es **manual**: sustituyes los binarios y **conservas** la base de datos.

### Diagrama del flujo

```text
PC cliente                          Tú (desarrollo)
─────────                          ────────────────
FerreSys\                          Directory.Build.props → 0.1.1
  FerreSys.Infrastructure.UI.exe
  ferresys.db  ←── NO TOCAR        publish.ps1 → ZIP v0.1.1
  (otros dll/native)
                 │
                 │  enviás ZIP
                 ▼
            1) Cerrar FerreSys
            2) Copiar ferresys.db a un respaldo
            3) Reemplazar .exe y archivos del paquete NUEVO
            4) Dejar (o restaurar) ferresys.db en la carpeta
            5) Abrir la app → versión nueva en el título
```

### Procedimiento paso a paso

1. En la PC del cliente, **cierra** FerreSys.
2. Respalda la DB:

   ```powershell
   Copy-Item "C:\FerreSys\ferresys.db" "C:\FerreSys\backups\ferresys-2026-07-14.db"
   ```

3. Extrae el ZIP nuevo en una carpeta temporal.
4. Copia **todos** los archivos del ZIP **excepto** `ferresys.db` (si viniera) sobre la instalación existente.
5. Verifica que `ferresys.db` sigue siendo el del cliente.
6. Abre la app y confirma la versión en el título (`vX.Y.Z`).

### Migraciones de base de datos

La app usa `CREATE TABLE IF NOT EXISTS` y, cuando hace falta, `ALTER TABLE` defensivos (por ejemplo columnas de pago en `Sales`). Eso permite:

- **Updates menores/patch** con esquema compatible: abrir la app nueva con la DB vieja suele bastar.
- **Updates mayores** que cambien el esquema de forma incompatible: documenta en el release notes y, si aplica, un script/migración manual antes de desplegar.

Si un update rompe la DB, restaura el respaldo del paso 2.

### Futuro (sugerido, aún no implementado)

Cuando quieras updates semiautomáticos:

| Opción | Idea |
|--------|------|
| **Velopack / Squirrel** | Instalador + feed de updates; descarga e instala en segundo plano |
| **GitHub Releases** | `publish.ps1` + release con el ZIP; un checador de versión compara con `Directory.Build.props` / endpoint |
| **Mover DB a AppData** | Separar datos de binarios facilita reemplazar solo el `.exe` |

Hasta entonces, el proceso ZIP + conservar `ferresys.db` es el contrato de actualización.

---

## Resumen rápido

| Objetivo | Comando / acción |
|----------|------------------|
| Correr en local | `dotnet run --project FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj` |
| Subir versión | Editar `Directory.Build.props` |
| Empaquetar | `.\scripts\publish.ps1` |
| Instalar en otra PC | Descomprimir ZIP → ejecutar `.exe` en carpeta de usuario |
| Actualizar | Respaldar `ferresys.db` → reemplazar binarios → conservar DB |

---

## Solución de problemas

| Problema | Qué revisar |
|----------|-------------|
| `dotnet` no se reconoce | Instalar SDK .NET 10 y reiniciar la terminal |
| La app no guarda datos | ¿Está en Program Files? Mueve la carpeta a `%LOCALAPPDATA%\FerreSys` |
| Al actualizar “desaparecieron” productos | Se sobrescribió `ferresys.db`; restaura el respaldo |
| SmartScreen bloquea el exe | Firma de código o “Ejecutar de todas formas” |
| Falta runtime (paquete framework-dependent) | Instalar [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |
