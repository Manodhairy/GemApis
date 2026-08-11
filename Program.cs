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

#region AddDbContext
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
#endregion

#region AutoMapper
builder.Services.AddAutoMapper(
    cfg => { },
    AppDomain.CurrentDomain.GetAssemblies()
);

#endregion

#region DI
builder.Services.AddScoped<
    IGeMBidRepository,
    GeMBidRepository
>();
builder.Services.AddScoped<
    IGeMBidService,
    GeMBidService
>();

builder.Services.AddScoped<
    IEmailService,
    EmailService
>();

#endregion

#region Email Confugure
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(
        "EmailSettings"
    )
);



builder.Services.AddHostedService<
    BidEmailBackgroundService
>();
#endregion

#region JWt
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

#endregion


builder.Services.AddAuthorization();


builder.Services.AddControllers();


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

#region CorsOrigin
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

#endregion


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