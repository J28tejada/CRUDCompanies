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


// Lista en memoria para almacenar compañías y empleados
List<Company> companies = new List<Company>();

// Obtener todas las compañías
app.MapGet("/compañias", () => Results.Ok(companies));

// Obtener una compañía por ID
app.MapGet("/compañias/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    return company is not null ? Results.Ok(company) : Results.NotFound($"No se encontró la compañía con ID {id}");
});

// Crear una nueva compañía
app.MapPost("/compañias", (Company company) =>
{
    company.Id = companies.Count + 1;
    companies.Add(company);
    return Results.Created($"/compañias/{company.Id}", company);
});

// Actualizar una compañía existente
app.MapPut("/compañias/{id}", (int id, Company updatedCompany) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    company.Name = updatedCompany.Name;
    company.Employees = updatedCompany.Employees;
    return Results.Ok(company);
});

// Eliminar una compañía por ID
app.MapDelete("/compañias/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {id}");

    companies.Remove(company);
    return Results.Ok($"Compañía con ID {id} eliminada");
});

// Agregar un empleado a una compañía
app.MapPost("/compañias/{companyId}/empleados", (int companyId, Employee employee) =>
{
    var company = companies.FirstOrDefault(c => c.Id == companyId);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {companyId}");

    employee.Id = company.Employees.Count + 1; 
    company.Employees.Add(employee);
    return Results.Created($"/compañias/{companyId}/empleados/{employee.Id}", employee);
});

// Eliminar un empleado de una compañía
app.MapDelete("/compañias/{companyId}/empleados/{employeeId}", (int companyId, int employeeId) =>
{
    var company = companies.FirstOrDefault(c => c.Id == companyId);
    if (company is null) return Results.NotFound($"No se encontró la compañía con ID {companyId}");

    var employee = company.Employees.FirstOrDefault(e => e.Id == employeeId);
    if (employee is null) return Results.NotFound($"No se encontró el empleado con ID {employeeId}");

    company.Employees.Remove(employee);
    return Results.Ok($"Empleado con ID {employeeId} eliminado de la compañía {company.Name}");
});

app.Run();

class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new();
}

class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}