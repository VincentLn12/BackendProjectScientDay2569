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
});
#endregion

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://coms.kru.ac.th",
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

//var pathBase = "/cs67/react/s14/rovmn";
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


app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireHost("*");

app.MapHub<MatchHub>("/matchHub");

app.MapFallbackToFile("index.html");


app.Run();