using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Auth;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Resend;
using System.Text;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ChatDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<ChatDbContext>()
.AddDefaultTokenProviders();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Jwt settings are missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Email:ApiKey"]
                 ?? throw new InvalidOperationException("Email:ApiKey is missing.");
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();

builder.Services.AddScoped<IUsernameGenerator, UsernameGenerator>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ChatApp API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT token from /api/auth/login. Do not type the word 'Bearer'."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
