using GemApi.BackgroundServices;
using GemApi.Data;
using GemApi.Models.Repository;
using GemApi.Services;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Microsoft.OpenApi.Models;

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

builder.Services.AddScoped<JwtService>();
// ---- JWT ----
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GemApi",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});


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
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();


