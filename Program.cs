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
builder.Services.AddScoped<IPuneBidAlertService, PuneBidAlertService>();
builder.Services.Configure<PuneBidAlertSettings>(
    builder.Configuration.GetSection("PuneBidAlert"));
builder.Services.AddHostedService<PuneBidAlertBackgroundService>();
builder.Services.AddScoped<IPuneBidEmailService, PuneBidEmailService>();

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

    // Ensure token matches the DB record
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var userIdStr = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var incomingToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();

            // FIX: Change int to long
            if (long.TryParse(userIdStr, out long userId))
            {
                var admin = await dbContext.Admins.FindAsync(userId);

                if (admin == null || string.IsNullOrEmpty(admin.Token) || admin.Token != incomingToken)
                {
                    context.Fail("Token has been revoked or expired.");
                }
            }
            else
            {
                context.Fail("Invalid user claim.");
            }
        }
    };
});

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
               "https://gemsbid.sdaemon.com" ,
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