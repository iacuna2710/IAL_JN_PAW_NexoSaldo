# Proyecto JN (PAW_Jueves)

Aplicación web de **gastos personales** hecha en ASP.NET Core 10:

- **PAW_Jueves**: WEB MVC (vistas Razor, plantilla Spark Admin). No se conecta a la BD: llama a la API.
- **PAW_Jueves_API**: API REST con Dapper y procedimientos almacenados sobre SQL Server. Autenticación con JWT.
- **BD**: respaldo inicial (`BD/Respaldo.sql`) y migraciones (`BD/Migraciones/*.sql`).

Funcionalidades: registro e inicio de sesión (BCrypt, JWT, límite de intentos), Mi cuenta (foto de perfil, datos),
Settings (contraseña, moneda, tema), bitácora de actividad, Lock Screen, categorías y movimientos con presupuestos
y alertas, dashboard con gráficos y un **asistente con IA (Gemini)**: chat y resumen del mes.

## 1. Base de datos

```bash
docker compose up -d db
docker compose up db-init
```

`db-init` restaura el respaldo si la BD no existe y después aplica **todas** las migraciones en orden
(son idempotentes: se pueden correr varias veces). Para ver la BD en SSMS: servidor `localhost,14330`, usuario `sa`.

La API en desarrollo (perfiles `http`, `https` y `Container` de Visual Studio) usa esa misma BD.

## 2. Clave de Gemini (asistente IA)

La clave **nunca** se escribe en el código ni en `appsettings.json`. Sin clave todo funciona menos el asistente,
que muestra "no configurado".

1. Cree una clave gratis en [Google AI Studio](https://aistudio.google.com/apikey).
2. Guárdela según cómo ejecute la API:

   **Visual Studio o `dotnet run`** (también sirve para el perfil Container, que monta los user-secrets):

   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "SU_CLAVE" --project PAW_Jueves_API/PAW_Jueves_API
   ```

   Queda en su perfil de Windows (`%APPDATA%\Microsoft\UserSecrets\...`), fuera del repositorio.

   **docker-compose**: copie `.env.example` como `.env` y complete `GEMINI_API_KEY`. El archivo `.env` está en `.gitignore`.

3. Reinicie la API.

El modelo se cambia sin tocar código con `Gemini:Modelo` (por defecto `gemini-3.8-flash`). Si el plan gratuito
le da error de cuota o de acceso con ese modelo, revise en AI Studio qué modelos tiene disponibles, por ejemplo:

```bash
dotnet user-secrets set "Gemini:Modelo" "NOMBRE_DEL_MODELO" --project PAW_Jueves_API/PAW_Jueves_API
```

**Privacidad:** a Gemini solo se envían totales agregados del mes (por categoría), nunca el nombre, correo,
identificación ni las descripciones de los movimientos. En el plan gratuito Google puede usar las consultas
para mejorar sus productos.

## 3. Ejecutar

- **Visual Studio**: abra las dos soluciones e inicie la API y la WEB (perfil `https` o `Container`).
- **Todo con Docker**: `docker compose up --build` → WEB en http://localhost:5051, API en http://localhost:5135/scalar.

## 4. Pruebas

```bash
dotnet test PAW_Jueves/PAW_Jueves.Tests
dotnet test PAW_Jueves_API/PAW_Jueves_API.Tests
```

Las pruebas de Gemini usan un servidor falso: no necesitan clave ni gastan cuota. El CI (GitHub Actions) las corre en cada push.
