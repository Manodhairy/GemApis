using GemApi.BackgroundServices;
using GemApi.Data;
using GemApi.Repository;
using GemApi.Repository.Interfaces;
using GemApi.Services;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );


builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseSqlServer(connectionString);
    }
);

builder.Services.AddAutoMapper(
    cfg => { },
    AppDomain.CurrentDomain.GetAssemblies()
);

builder.Services.AddScoped<
    IGeMBidRepository,
    GeMBidRepository>();

builder.Services.AddScoped<
    IGeMBidService,
    GeMBidService>();


builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(
        "EmailSettings")
);

builder.Services.AddScoped<
    IEmailService,
    EmailService>();



builder.Services.AddHostedService<
    BidEmailBackgroundService>();



builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactPolicy",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});



var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ReactPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();