using Audit.Application;
using Audit.Infrastructure;
using Audit.Presentation.Controllers;
using BuildingBlocks.Infrastructure.Persistence;
using Chat.Application;
using Chat.Infrastructure;
using Chat.Presentation;
using Chat.Presentation.Controllers;
using Chat.Presentation.Hubs;
using Bonus.Application;
using Bonus.Infrastructure;
using Bonus.Presentation.Controllers;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Presentation.Controllers;
using Clients.Application;
using Clients.Infrastructure;
using Clients.Presentation.Controllers;
using CrmSystem.Middleware;
using Deals.Application;
using Deals.Infrastructure;
using Deals.Presentation.Controllers;
using Email.Application;
using Email.Infrastructure;
using Email.Presentation.Controllers;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Security;
using Identity.Presentation.Controllers;
using Infrastructure;
using Infrastructure.Email;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Warehouse.Application;
using Warehouse.Infrastructure;
using Warehouse.Presentation.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<SystemAdminOptions>(builder.Configuration.GetSection("SystemAdmin"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AppDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuditApplication();
builder.Services.AddIdentityApplication();
builder.Services.AddChatApplication();
builder.Services.AddBonusApplication();
builder.Services.AddClientsApplication();
builder.Services.AddCatalogApplication();
builder.Services.AddDealsApplication();
builder.Services.AddEmailApplication();
builder.Services.AddWarehouseApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuditInfrastructure();
builder.Services.AddIdentityInfrastructure();
builder.Services.AddChatInfrastructure();
builder.Services.AddChatPresentation();
builder.Services.AddBonusInfrastructure();
builder.Services.AddClientsInfrastructure();
builder.Services.AddCatalogInfrastructure();
builder.Services.AddDealsInfrastructure();
builder.Services.AddEmailInfrastructure(builder.Configuration);
builder.Services.AddWarehouseInfrastructure();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AuditLogsController).Assembly)
    .AddApplicationPart(typeof(PublicIdentityController).Assembly)
    .AddApplicationPart(typeof(ChatConversationsController).Assembly)
    .AddApplicationPart(typeof(BonusSettingsController).Assembly)
    .AddApplicationPart(typeof(ClientsController).Assembly)
    .AddApplicationPart(typeof(CategoriesController).Assembly)
    .AddApplicationPart(typeof(DealsController).Assembly)
    .AddApplicationPart(typeof(EmailSettingsController).Assembly)
    .AddApplicationPart(typeof(StoragesController).Assembly);

builder.Services.AddSignalR();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSystemAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsSystemAdmin", "true");
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

public partial class Program { }
