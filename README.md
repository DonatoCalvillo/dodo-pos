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
├── FerreSys.Licensing/                   # Validación RSA de la licencia
├── FerreSys.LicenseIssuer/               # Emite archivos .lic (no se instala en la ferretería)
├── Directory.Build.props                 # Versión de la aplicación
├── build-release.ps1                     # Instalador Velopack para clientes
├── scripts/publish.ps1                   # ZIP portable
└── FerreSys.slnx
```

La base de datos `ferresys.db` se crea en `%LOCALAPPDATA%\FerreSys` la primera vez que se abre la app. No viaja junto al ejecutable.

---

## Licencias y respaldos

No hace falta Supabase ni otro servidor. La ferretería cobra sin internet. Sin un archivo de licencia válido, el programa abre en plan gratuito: hasta 200 productos y sin paquete cifrado para la nube.

La licencia dura **30 días**, más **5 de gracia**. Cada mes hay que generar otro archivo y mandárselo al cliente (WhatsApp alcanza). Si no paga, no se le manda el siguiente y, al vencer, vuelve al plan gratuito. El mostrador sigue abriendo.

La llave privada vive solo en tu PC:

```text
%USERPROFILE%\.ferresys\licensing\private.xml
```

No la copies al instalador ni al repositorio. La app solo trae la llave pública. No ejecutes `keygen --force`: eso crea otra privada y las licencias ya emitidas dejan de servir.

### Generar la licencia del mes

1. En la ferretería: Datos del negocio → **Administrar licencia** → **Copiar**. Ese texto es el identificador de esa computadora.
2. En tu PC, desde la carpeta del repo:

```powershell
dotnet run --project FerreSys.LicenseIssuer -- issue --email ferreteria@gmail.com --name "Ferretería El MVP" --hardware PEGA_AQUI_EL_ID
```

3. El comando escribe `license.lic` en la carpeta actual e imprime un código `FS-XXXX-XXXX` y el identificador de la licencia.
4. Envía ese `.lic`. En la misma pantalla, el cliente usa **Importar archivo .lic**.

Opciones útiles:

```powershell
dotnet run --project FerreSys.LicenseIssuer -- issue --email ferreteria@gmail.com --name "Ferretería El MVP" --hardware PEGA_AQUI_EL_ID --days 30 --output .\license.lic
dotnet run --project FerreSys.LicenseIssuer -- revoke --code FS-XXXX-XXXX
```

`revoke` impide emitir o renovar ese código en tu PC. Un `.lic` que el cliente ya importó sigue vigente hasta su fecha.

El archivo queda en la ferretería en `%LOCALAPPDATA%\FerreSys\license.lic`.

### Respaldos

| Copia | Cuándo | Dónde |
|-------|--------|--------|
| Base de datos | Cada vez que se cierra FerreSys | `Documentos\FerreSys_Backups` en esa misma PC |
| Paquete cifrado | Solo con licencia Premium, al cerrar o al guardar el Corte Z | `%LOCALAPPDATA%\FerreSys\pending-backups` |

Esas copias no se suben a la nube. Si se daña ese disco, no hay otra copia fuera de la ferretería.

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
3. Arma el instalador con `.\build-release.ps1` (y, si quieres el ZIP portable, `scripts\publish.ps1`).
4. Crea un tag git:

```powershell
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin v0.1.0
```

5. Pasa a cada cliente `releases\FerreSysApp-win-Setup.exe`. El ZIP `artifacts\FerreSys-v0.1.0-win-x64.zip` queda como copia portable.

---

## Instalador para clientes

Esto produce el programa que se les pasa. No incluye la llave privada ni una licencia: cada ferretería recibe después su `.lic`.

Hace falta el SDK de .NET 10 y la herramienta de Velopack, una sola vez:

```powershell
dotnet tool install -g vpk
```

Luego, desde la carpeta del repo:

```powershell
.\build-release.ps1
```

El script publica `FerreSys.exe` (Windows 64-bit, con el runtime incluido; el cliente no instala .NET) y arma el instalador. El archivo para enviar es:

```text
releases\FerreSysApp-win-Setup.exe
```

En la ferretería se ejecuta ese Setup. Instala por usuario, sin copiar la base de otra PC. Los datos quedan en `%LOCALAPPDATA%\FerreSys`.

La versión del instalador está escrita en `build-release.ps1` (`-v 0.1.0`). Al soltar una versión nueva, súbela ahí y también en `Directory.Build.props`, con el mismo número.

El ZIP portable (`.\scripts\publish.ps1`) es otra forma de llevar la carpeta publicada. Para un cliente, el archivo de instalación es el Setup de arriba.

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

3. Ejecuta `FerreSys.exe`.
4. (Opcional) Crea un acceso directo en el Escritorio apuntando a ese `.exe`.
5. La primera ejecución crea `ferresys.db` en `%LOCALAPPDATA%\FerreSys`, no junto al ejecutable.

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
| **Velopack** | El instalador ya se arma con `.\build-release.ps1`. La descarga automática sigue apagada: `FeedUrl` en `UpdateSettings.cs` está vacío |
| **GitHub Releases** | `publish.ps1` + release con el ZIP; un checador de versión compara con `Directory.Build.props` / endpoint |
| **Mover DB a AppData** | Separar datos de binarios facilita reemplazar solo el `.exe` |

Hasta entonces, el proceso ZIP + conservar `ferresys.db` es el contrato de actualización.

---

## Resumen rápido

| Objetivo | Comando / acción |
|----------|------------------|
| Correr en local | `dotnet run --project FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj` |
| Subir versión | Editar `Directory.Build.props` |
| Empaquetar instalador | `.\build-release.ps1` → `releases\FerreSysApp-win-Setup.exe` |
| Empaquetar ZIP | `.\scripts\publish.ps1` |
| Instalar en otra PC | Ejecutar `FerreSysApp-win-Setup.exe` |
| Generar licencia del mes | `dotnet run --project FerreSys.LicenseIssuer -- issue --email ... --name ... --hardware ...` |
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
