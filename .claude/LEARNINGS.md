# Learnings & Troubleshooting

This document captures problems encountered during development, their root causes, and solutions. These are real issues that happened during this project - documenting them helps avoid repeating them and helps others who encounter similar issues.

**Format:**
- **Story:** Which story the issue occurred in
- **Problem:** What went wrong
- **Root Cause:** Why it happened
- **Solution:** How it was fixed
- **Prevention:** How to avoid it next time

---

## Story 001: Initialize .NET Project Structure

### Issue 1: Minimal API Created Instead of Controller-Based API

**Problem:**
Ran `dotnet new webapi -n MarsVista.Api -o src/MarsVista.Api` but got a minimal API (endpoints in Program.cs with lambda functions) instead of a controller-based API with a `Controllers/` directory.

**Root Cause:**
In .NET 8 and .NET 9, the `dotnet new webapi` template defaults to creating minimal APIs. The controller-based approach requires an explicit flag.

**What We Saw:**
- No `Controllers/` directory
- `Program.cs` had `app.MapGet("/weatherforecast", () => { ... })` instead of controller registration
- `Program.cs` had `builder.Services.AddOpenApi()` instead of `AddControllers()`

**Solution:**
Delete the project and recreate with the `--use-controllers` flag:
```bash
rm -rf src/MarsVista.Api
dotnet new webapi -n MarsVista.Api -o src/MarsVista.Api --use-controllers
dotnet sln add src/MarsVista.Api/MarsVista.Api.csproj
```

**Prevention:**
- Always use `--use-controllers` flag when creating Web API projects if you want controller-based architecture
- Check for `Controllers/` directory immediately after project creation
- Verify `Program.cs` contains `AddControllers()` and `MapControllers()`

**Updated Story:**
Story 001 was updated with the correct command and a warning about this issue.

**References:**
- [Choose between controller-based and minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis)

---

### Issue 2: Can't Connect to Server - Port Confusion

**Problem:**
Server was running but couldn't access it at `https://localhost:7055` or `https://localhost:7055/swagger`. Got connection refused errors and 404s.

**Root Cause:**
The server was running on the `http` profile (port 5127) instead of the `https` profile (port 7055). Multiple related issues:

1. **Launch profile selection:** `dotnet run` defaults to the first profile in `launchSettings.json`, which was the `http` profile
2. **Wrong port:** Trying to access port 7055 when server was on port 5127
3. **Wrong protocol:** Trying HTTPS when server was running HTTP only

**What We Saw:**
```bash
netstat -tlnp | grep 5127
tcp  0  0  127.0.0.1:5127  0.0.0.0:*  LISTEN  2482994/MarsVista.A
```

Server was only listening on port 5127 (HTTP), not 7055 (HTTPS).

**How We Debugged:**
1. Checked if ports were listening: `netstat -tlnp | grep -E ":(7055|5127)"`
2. Found only port 5127 was active
3. Tested the correct port: `curl http://localhost:5127/weatherforecast` - SUCCESS!

**Solution:**
Either:

**Option A:** Use the correct port for the running profile
```bash
# Server is running on HTTP profile
http://localhost:5127/weatherforecast
```

**Option B:** Specify the HTTPS profile when starting
```bash
dotnet run --project src/MarsVista.Api --launch-profile https
```

Then use:
```bash
https://localhost:7055/weatherforecast
```

**Prevention:**
- Check which ports are listening: `netstat -tlnp | grep <port>` or `ss -tlnp | grep <port>`
- Look at startup logs - they show which URLs the server is listening on
- Understand launch profiles in `Properties/launchSettings.json`
- Default profile can change - always verify what's running

**Launch Profiles Explained:**
The `launchSettings.json` file defines different ways to run the app:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5127"
    },
    "https": {
      "applicationUrl": "https://localhost:7055;http://localhost:5127"
    }
  }
}
```

- `dotnet run` uses the first profile (usually `http`)
- `dotnet run --launch-profile https` uses the specified profile
- In Visual Studio/Rider, you can select the profile in the UI

**References:**
- [Launch profiles in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments#development-and-launchsettingsjson)

---

### Issue 3: No Swagger UI at /swagger Endpoint

**Problem:**
Tried to access `http://localhost:5127/swagger` but got 404 Not Found.

**Root Cause:**
.NET 9's `dotnet new webapi` template uses the new `Microsoft.AspNetCore.OpenApi` package, which generates OpenAPI specifications but does **not** include Swagger UI (the interactive documentation interface).

**What We Have:**
- ✅ OpenAPI spec at `/openapi/v1.json`
- ❌ No Swagger UI at `/swagger`

**What We Saw in Program.cs:**
```csharp
builder.Services.AddOpenApi();    // Not Swagger
app.MapOpenApi();                 // Maps JSON spec, not UI
```

**Verification:**
```bash
# OpenAPI spec works:
curl http://localhost:5127/openapi/v1.json

# Swagger UI doesn't exist:
curl http://localhost:5127/swagger
# 404 Not Found
```

**Solution (if Swagger UI needed):**
Add Swashbuckle package:
```bash
dotnet add package Swashbuckle.AspNetCore
```

Replace in `Program.cs`:
```csharp
// Remove:
builder.Services.AddOpenApi();
app.MapOpenApi();

// Add:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

**Decision Made:**
For Story 001, we decided **not** to add Swagger UI because:
- The API works perfectly without it
- Can test with curl or browser
- OpenAPI spec is available for API clients
- Can add Swagger later if needed
- Keeps dependencies minimal

**Prevention:**
- Understand that OpenAPI spec ≠ Swagger UI
- .NET 9 template provides spec generation, not UI
- Check what packages are actually installed
- `/swagger` endpoint requires `Swashbuckle.AspNetCore` package

**The Terminology:**
- **OpenAPI:** A specification format for describing REST APIs (formerly called Swagger Spec)
- **Swagger:** A set of tools for working with OpenAPI, including Swagger UI
- **Swagger UI:** A web interface that renders OpenAPI specs as interactive documentation
- **Swashbuckle:** The .NET library that implements Swagger/OpenAPI support

**References:**
- [OpenAPI vs Swagger - What's the difference?](https://swagger.io/blog/api-strategy/difference-between-swagger-and-openapi/)
- [Microsoft.AspNetCore.OpenApi package](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [Swashbuckle.AspNetCore package](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

---

### Issue 4: HTTPS Redirect Warning

**Problem:**
Saw warning in logs: `Failed to determine the https port for redirect`

**Root Cause:**
`Program.cs` contains `app.UseHttpsRedirection()` which tries to redirect HTTP requests to HTTPS. When running with the `http` profile (which only has HTTP, no HTTPS port), the middleware can't determine where to redirect to.

**What This Means:**
The warning is harmless for development but indicates a configuration mismatch:
- Code expects HTTPS to be available
- Running profile only provides HTTP

**Solution Options:**

**Option A:** Run with HTTPS profile (recommended)
```bash
dotnet run --launch-profile https
```

**Option B:** Remove HTTPS redirection for development
```csharp
// Only redirect in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

**Option C:** Configure explicit HTTPS redirect port
```csharp
app.UseHttpsRedirection(new HttpsRedirectionOptions
{
    HttpsPort = 7055
});
```

**Decision Made:**
For Story 001, we accepted the warning and will use the HTTPS profile when needed. In production, HTTPS redirect is important for security.

**Prevention:**
- Match your middleware configuration to your launch profile
- Use HTTPS profile in development to mirror production
- Understand that `UseHttpsRedirection()` requires HTTPS to be configured

**References:**
- [Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl)

---

## General Debugging Tips

### When Server Won't Connect

1. **Check if it's actually running:**
   ```bash
   ps aux | grep dotnet
   ```

2. **Check what ports are listening:**
   ```bash
   netstat -tlnp | grep <port>
   # or
   ss -tlnp | grep <port>
   ```

3. **Check the startup logs:**
   Look for lines like:
   ```
   Now listening on: http://localhost:5127
   Now listening on: https://localhost:7055
   ```

4. **Try curl before browser:**
   ```bash
   curl http://localhost:5127/weatherforecast
   curl -k https://localhost:7055/weatherforecast
   ```
   The `-k` flag ignores certificate errors for development.

5. **Check firewall/security:**
   - Is something blocking the port?
   - Are you trying to access from a different machine?

### When Endpoint Returns 404

1. **Check the route:**
   - Controller route: `[Route("[controller]")]` means `/WeatherForecast`
   - Not `/weatherforecast` (case matters on Linux!)

2. **Check controller registration:**
   - `builder.Services.AddControllers()`
   - `app.MapControllers()`

3. **List available endpoints:**
   ```bash
   # OpenAPI spec shows all routes
   curl http://localhost:5127/openapi/v1.json | jq '.paths'
   ```

### When Template Generates Wrong Code

1. **Check .NET version:**
   ```bash
   dotnet --version
   ```

2. **List available templates:**
   ```bash
   dotnet new list
   ```

3. **See template options:**
   ```bash
   dotnet new webapi --help
   ```

4. **Check what was actually created:**
   ```bash
   ls -la src/YourProject/
   cat src/YourProject/Program.cs
   ```

---

## Key Takeaways from Story 001

1. **.NET defaults have changed** - What worked in .NET 6/7 might need flags in .NET 8/9
2. **Verify immediately** - Check that what was generated matches what you expected
3. **Port confusion is common** - Always verify which port the server is actually on
4. **Logs are your friend** - Startup logs tell you everything you need to know
5. **OpenAPI ≠ Swagger UI** - Modern .NET provides specs, not UI by default
6. **Launch profiles matter** - Default profile might not be what you expect

---

## Template for Future Issues

```markdown
### Issue N: <Short Description>

**Problem:**
<What went wrong from user perspective>

**Root Cause:**
<Technical explanation of why it happened>

**What We Saw:**
<Error messages, logs, symptoms>

**Solution:**
<How it was fixed>

**Prevention:**
<How to avoid this in future>

**References:**
<Links to docs, articles, etc.>
```

---

**Note:** This file will grow as we encounter and solve more issues. Every problem is a learning opportunity!
