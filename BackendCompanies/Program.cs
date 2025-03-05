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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/compañias", () =>
{
    return ("compañias y empleados");
})
.WithName("compañias")
.WithOpenApi();

app.MapPost("/compañiasPost", () =>
{
    return ("Enviar compañias y empleados");
})
.WithName("compañiasPost")
.WithOpenApi();

app.MapPut("/compañiasPut", () =>
{
    return ("compañias y empleados");
})
.WithName("compañiasPut")
.WithOpenApi();

app.MapDelete("/compañiasDelete", () =>
{
    return ("compañias y empleados");
})
.WithName("compañiasDelete")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
