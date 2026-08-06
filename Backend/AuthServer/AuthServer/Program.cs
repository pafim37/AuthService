using AuthServer.Database;
using AuthServer.Database.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//IConfigurationSection jwtSettings = builder.Configuration.GetSection("Jwt");
//string? signingKey = jwtSettings["AuthServiceKey"];
//if (string.IsNullOrWhiteSpace(signingKey))
//{
//    throw new InvalidOperationException("JWT signing key is not configured. Please set Jwt:AuthServiceKey in configuration or environment variables.");
//}

builder.Services.AddDbContext<AuthContext>(options => 
                options.UseSqlServer(builder.Configuration["ConnectionStrings:Default"]),
                ServiceLifetime.Scoped);


builder.Services.AddScoped<IPrivilegeRepository, PrivilegeRepository>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
