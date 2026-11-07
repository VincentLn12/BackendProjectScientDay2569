global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
global using ProjectScientDay2568.Models;
global using System.Net;
global using Microsoft.EntityFrameworkCore;
global using ProjectScientDay2568.Data;
global using System.ComponentModel.DataAnnotations;
global using ProjectScientDay2568.Interface;
global using AutoMapper;
global using ProjectScientDay2568.Service;
global using Microsoft.AspNetCore.Mvc;
global using ProjectScientDay2568.Models.Dtos.Competition;
using ProjectScientDay2568.Mappings;
using ProjectScientDay2568.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using ProjectScientDay2568.Hubs;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddScoped<YearService>();
builder.Services.AddScoped<CompetitionTypeService>();
builder.Services.AddScoped<CompetitionService>();
builder.Services.AddScoped<CompetitionYearService>();
builder.Services.AddScoped<EducationLevelService>();
builder.Services.AddScoped<CompetitionlevelService>();
builder.Services.AddScoped<RewardTypeService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<SchoolService>();
builder.Services.AddScoped<CertificateConfigService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<TeamProjectService>();


builder.Services.AddSignalR();


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.AllowedForNewUsers = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
});

#region JWT
var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");
builder.Services.AddAuthentication(u =>
{
    u.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    u.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(u =>
{
    u.RequireHttpsMetadata = false;
    u.SaveToken = true;
    u.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
    // บัญชีที่ถูกระงับ (LockoutEnd ในอนาคต) ใช้ token ที่ออกไปแล้วต่อไม่ได้ทันที
    u.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var userId = ctx.Principal?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return;
            var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var threshold = AccountSuspension.Threshold;
            var suspended = await db.ApplicationUsers.AsNoTracking()
                .AnyAsync(x => x.Id == userId && x.LockoutEnd != null && x.LockoutEnd > threshold);
            if (suspended) ctx.Fail("บัญชีนี้ถูกระงับการใช้งาน");
        }
    };
});
#endregion

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://coms.kru.ac.th",
                "https://coms.kru.ac.th",
                "http://10.103.0.15",
                "https://10.103.0.15"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDataProtection()
  .SetApplicationName("ProjectScientDay2568")
  .PersistKeysToFileSystem(new DirectoryInfo(
     Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")));


builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
{
    o.TokenLifespan = TimeSpan.FromHours(2);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
            "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
            "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
           new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseForwardedHeaders();

//app.UsePathBase("/RovMN");

//app.UsePathBase(pathBase);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseWebSockets();


app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseRateLimiter(); // หลัง authentication เพื่อนับโควตาต่อบัญชีได้
app.UseAuthorization();

app.MapControllers().RequireHost("*");

app.MapHub<MatchHub>("/matchHub");

app.MapFallbackToFile("index.html");


app.Run();