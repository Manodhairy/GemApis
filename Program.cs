using GemApi.BackgroundServices;
using GemApi.Data;
using GemApi.Repository;
using GemApi.Repository.Interfaces;
using GemApi.Services;
using GemApi.Services.Interfaces;
using GemApi.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DATABASE
// ======================================================

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

// ======================================================
// AUTOMAPPER
// ======================================================

builder.Services.AddAutoMapper(
    cfg => { },
    AppDomain.CurrentDomain.GetAssemblies()
);

// ======================================================
// REPOSITORY
// ======================================================

builder.Services.AddScoped<
    IGeMBidRepository,
    GeMBidRepository
>();

// ======================================================
// SERVICES
// ======================================================

builder.Services.AddScoped<
    IGeMBidService,
    GeMBidService
>();

// ======================================================
// EMAIL SETTINGS
// ======================================================
//
// This loads:
// appsettings.json
// +
// User Secrets
//
// User Secret:
// EmailSettings:ApiKey
//
// Your EmailService receives it through:
// IOptions<EmailSettings>
// ======================================================

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(
        "EmailSettings"
    )
);

// ======================================================
// EMAIL SERVICE
// ======================================================

builder.Services.AddScoped<
    IEmailService,
    EmailService
>();

// ======================================================
// BACKGROUND EMAIL SERVICE
// ======================================================

builder.Services.AddHostedService<
    BidEmailBackgroundService
>();

// ======================================================
// JWT
// ======================================================

builder.Services.AddScoped<JwtService>();
var jwtKey =
    builder.Configuration["Jwt:Key"];

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];


if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is not configured."
    );
}

builder.Services.AddAuthentication(
    options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    }
)
.AddJwtBearer(
    options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey
                        )
                    ),

                ClockSkew =
                    TimeSpan.Zero
            };
    }
);

builder.Services.AddAuthorization();

// ======================================================
// CONTROLLERS
// ======================================================

builder.Services.AddControllers();

// ======================================================
// SWAGGER
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "GemApi",
                Version = "v1"
            }
        );

        // ----------------------------------------------
        // JWT Swagger Authentication
        // ----------------------------------------------

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",

                Type = SecuritySchemeType.Http,

                Scheme = "Bearer",

                BearerFormat = "JWT",

                In = ParameterLocation.Header,

                Description =
                    "Enter your JWT token."
            }
        );

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference =
                            new OpenApiReference
                            {
                                Type =
                                    ReferenceType.SecurityScheme,

                                Id = "Bearer"
                            }
                    },

                    Array.Empty<string>()
                }
            }
        );
    }
);

// ======================================================
// CORS
// ======================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "ReactPolicy",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        );
    }
);

// ======================================================
// BUILD APP
// ======================================================

var app = builder.Build();

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ======================================================
// MIDDLEWARE
// ======================================================

app.UseHttpsRedirection();

app.UseCors("ReactPolicy");

app.UseAuthentication();

app.UseAuthorization();

// ======================================================
// CONTROLLERS
// ======================================================

app.MapControllers();

// ======================================================
// RUN
// ======================================================

app.Run();