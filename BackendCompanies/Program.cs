using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var issuer = "mi-real-issuer";
var audience = "mi-real-audience";
var secretKey = "mi-real-secret-key-super-hiper-mega-secreto-y-larga-y-dificil-de-adivinar";

string GenerateToken(string username, string role, int expireMinutes = 60)
{
    var Claims = new[]
    {
        new Claim (JwtRegisteredClaimNames.Sub, username),
        new Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim (JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: Claims,
        expires: DateTime.UtcNow.AddMinutes(expireMinutes),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
        


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "API Segura",
        Version = "v1",
        Description = "API con autenticación JWT"
    });
    //Definir el esquema de seguridad
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT de esta manera: Bearer {token}"
    });
    // Usar el esquema de seguridad
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Urls.Add("http://localhost:5040");

// Lista en memoria para almacenar compañías y empleados
List<Company> companies = new List<Company>();

// Obtener todas las compañías
app.MapGet("/companies", async (AppDbContext db) => 
    await db.Companies.ToListAsync())
.RequireAuthorization();

// Obtener una compañía por ID
app.MapGet("/companies/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    return company is not null ? Results.Ok(company) : Results.NotFound($"No se encontró la compañía con ID {id}");
});

app.MapGet("/me", (HttpContext httpContext) =>
{
    var user = httpContext.User.Identity?.Name;
    return user is not null ? Results.Ok($"Usuario autenticado: {user}") : Results.Unauthorized();
}).RequireAuthorization();


app.MapPost("/login", (User user) =>
{
    // Aquí se debe validar contra una base de datos en un sistema real
    if (user.Username == "admin" && user.Password == "admin")
    {
        var token = GenerateToken(user.Username, "admin");
        return Results.Ok(new { token });
    }
    return Results.Unauthorized();
});

// Crear una nueva compañía
app.MapPost("/companies", async (CompanyRequest companyRequest, AppDbContext db) =>
{
    var company = new Company
    {
        Id = companies.Count + 1, // Generar ID simple
        Name = companyRequest.Name
    };
    db.Companies.Add(company);
    await db.SaveChangesAsync();
    companies.Add(company);
    return Results.Created($"/companies/{company.Id}", company);
});


// Actualizar una compañía existente
app.MapPut("/companies/{id}", (int id, Company updatedCompany) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    company.Name = updatedCompany.Name;
    company.Employees = updatedCompany.Employees;
    return Results.Ok(company);
});

// Agregar un empleado a una compañía
app.MapPost("/companies/{companyId}/employees", (int companyId, EmployeeRequest employeeRequest) =>
{
    var company = companies.FirstOrDefault(c => c.Id == companyId);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {companyId}");

    var employee = new Employee
    {
        Id = company.Employees.Count + 1,
        Name = employeeRequest.Name,
        Position = employeeRequest.Position
    };
    company.Employees ??= new List<Employee>();
    company.Employees.Add(employee);

    return Results.Created($"/companies/{companyId}/employee/{employee.Id}", employee);
});

// Eliminar un empleado de una compañía
app.MapDelete("/companies/{companyId}/employes/{employeeId}", (int companyId, int employeeId) =>
{
    var company = companies.FirstOrDefault(c => c.Id == companyId);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {companyId}");

    var employee = company.Employees.FirstOrDefault(e => e.Id == employeeId);
    if (employee is null) return Results.NotFound($"No se encontró el empleado con ID {employeeId}");

    company.Employees.Remove(employee);
    return Results.Ok($"Empleado con ID {employeeId} eliminado de la compañía {company.Name}");
});


// No permitir eliminar una compañía si tiene empleados
app.MapDelete("/companies/{id}", async (int id, AppDbContext db) =>
{
    var company = await db.Companies.Include(c => c.Employees).FirstOrDefaultAsync(c => c.Id == id);
    if (company == null)
    {
        return Results.NotFound($"No se encontró la compañía con ID {id}");
    }

    if (company.Employees.Any())
    {
        return Results.BadRequest($"No se puede eliminar la compañía {id} porque tiene empleados asociados. Usa el endpoint DELETE /companies/{id}/force si deseas eliminarla con sus empleados.");
    }

    db.Companies.Remove(company);
    await db.SaveChangesAsync();

    return Results.Ok($"Compañía {id} eliminada exitosamente.");
});


// Permitir eliminar una compañía junto con todos sus empleados
app.MapDelete("/companies/{id}/force", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    companies.Remove(company);
    return Results.Ok($"Compañía {id} y todos sus empleados eliminados exitosamente.");
});

app.Run();

class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new(); // Relación Uno a Muchos
}

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

class CompanyRequest
{
    public string Name { get; set; } = string.Empty;
}

class EmployeeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}