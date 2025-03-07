var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Urls.Add("http://localhost:5040");

// Lista en memoria para almacenar compañías y empleados
List<Company> companies = new List<Company>();

// Obtener todas las compañías
app.MapGet("/companies", () => Results.Ok(companies));

// Obtener una compañía por ID
app.MapGet("/companies/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    return company is not null ? Results.Ok(company) : Results.NotFound($"No se encontró la compañía con ID {id}");
});

// Crear una nueva compañía
app.MapPost("/companies", (CompanyRequest companyRequest) =>
{
    var company = new Company
    {
        Id = companies.Count + 1, // Generar ID simple
        Name = companyRequest.Name
    };
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
app.MapDelete("/companies/{companyId}/employess/{employeeId}", (int companyId, int employeeId) =>
{
    var company = companies.FirstOrDefault(c => c.Id == companyId);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {companyId}");

    var employee = company.Employees.FirstOrDefault(e => e.Id == employeeId);
    if (employee is null) return Results.NotFound($"No se encontró el empleado con ID {employeeId}");

    company.Employees.Remove(employee);
    return Results.Ok($"Empleado con ID {employeeId} eliminado de la compañía {company.Name}");
});


// DELETE: No permitir eliminar una compañía si tiene empleados
app.MapDelete("/companies/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    if (company.Employees.Any())
    {
        return Results.BadRequest($"No se puede eliminar la compañía {id} porque tiene empleados asociados. Usa el endpoint DELETE /companies/{id}/force si deseas eliminarla con sus empleados.");
    }

    companies.Remove(company);
    return Results.Ok($"Compañía {id} eliminada exitosamente.");
});

// DELETE: Permitir eliminar una compañía junto con todos sus empleados
app.MapDelete("/companies/{id}/force", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    companies.Remove(company);
    return Results.Ok($"Compañía {id} y todos sus empleados eliminados exitosamente.");
});

app.Run();

class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new(); // Relación Uno a Muchos
}

class Employee
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