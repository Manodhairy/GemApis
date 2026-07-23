using GemApi.Data;
using GemApi.Repository;
using GemApi.Repository.Interfaces;
using GemApi.Services;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString=builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IGeMBidRepository, GeMBidRepository>();
builder.Services.AddScoped<IGeMBidService, GeMBidService>();

builder.Services.AddControllers();
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

app.UseAuthorization();

app.MapControllers();

app.Run();


