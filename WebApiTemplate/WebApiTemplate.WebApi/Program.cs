using WebApiTemplate.Persistence;
using WebApiTemplate.Application.Interfaces;
using WebApiTemplate.Application.Services;
using Microsoft.EntityFrameworkCore;

const string BaseDirectory = "[BaseDirectory]";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration.GetConnectionString("ConnectionString");

if (connectionString.Contains(BaseDirectory))
{
    string contentRootPath = Directory.GetCurrentDirectory();
    connectionString = connectionString.Replace(BaseDirectory, contentRootPath);
}

builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connectionString));

builder.Services
    .AddScoped<IProductService, ProductService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    DatabaseContext databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    DatabaseInitializer.Initialize(databaseContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        x.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
    });
    app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
