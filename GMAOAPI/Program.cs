using GMAOAPI.Data;
using GMAOAPI.Models.Entities;
using GMAOAPI.Repository;
using GMAOAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Scrutor;

using Serilog.Sinks.MSSqlServer;
using static Serilog.Sinks.MSSqlServer.ColumnOptions;
using System.Collections.ObjectModel;
using System.Data;
using GMAOAPI.Services.SeriLog;
using GMAOAPI.Services.Token;
using GMAOAPI.Services.Caching;
using GMAOAPI.DTOs;
using StackExchange.Redis;
using System.Text;
using GMAOAPI.Models.Enumerations;
using System.Text.Json.Serialization;
using GMAOAPI.Services.BackgroundServices;
using GMAOAPI.Services.EmailService;
using Newtonsoft.Json.Converters;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;

namespace GMAOAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
            });
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<GmaoDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("GmaoConnection")));

            builder.Services.AddStackExchangeRedisCache(option =>
            {
                option.Configuration = builder.Configuration.GetConnectionString("Redis");
                option.InstanceName = "GMAO_";
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                string redisConnection = builder.Configuration.GetConnectionString("Redis");

                return ConnectionMultiplexer.Connect(redisConnection);
            });


            builder.Services.AddIdentity<Utilisateur, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;


            })
            .AddEntityFrameworkStores<GmaoDbContext>()
            .AddErrorDescriber<FrenchIdentityErrorDescriber>()
            .AddDefaultTokenProviders();


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                options.DefaultChallengeScheme =
                options.DefaultForbidScheme =
                options.DefaultScheme =
                options.DefaultSignInScheme =
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigninKey"])),

                };
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services
           .AddScoped<IAuditService, AuditService>()
            .AddScoped<IEquipementService, EquipementService>()
            .AddScoped<IInterventionService, InterventionService>()
            .AddScoped<IInterventionPieceDetacheeService, InterventionPieceDetacheeService>()
            .AddScoped<IInterventionTechnicienService, InterventionTechnicienService>()
            .AddScoped<INotificationService, NotificationService>()
            .AddScoped<IPieceDetacheeService, PieceDetacheeService>()
            .AddScoped<IPlanificationService, PlanificationService>()
            .AddScoped<IRapportService, RapportService>()
            .AddScoped<IUtilisateurService, UtilisateurService>()
            .AddScoped<IFournisseurService, FournisseurService>();

            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddSingleton<ISerilogService, SerilogService>();
            builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

            builder.Services.AddScoped<UserManager<Utilisateur>>();
            builder.Services.AddScoped<SignInManager<Utilisateur>>();
            builder.Services.AddScoped<RoleManager<IdentityRole>>();
            builder.Services.AddHostedService<PlanificationScheduler>();
            builder.Services.AddScoped<TechnicienDashboardService>();
            builder.Services.AddScoped<ResponsableDashboardService>();


            var sendinblueKey = builder.Configuration["Sendinblue:ApiKey"];

            builder.Services
               .AddSingleton<SmtpEmailSender>()
               .AddSingleton<SendinblueEmailSender>(_ => new SendinblueEmailSender(sendinblueKey));
            builder.Services.AddSingleton<IEmailSender>(sp =>
                        sp.GetRequiredService<SendinblueEmailSender>()
                    );

            //var columnOptions = new ColumnOptions();
            //columnOptions.AdditionalColumns = new Collection<SqlColumn>
            //{
            //    new SqlColumn { ColumnName = "UserName", DataType = SqlDbType.NVarChar,DataLength =255, AllowNull = true },
            //    new SqlColumn { ColumnName = "ActionEffectuee", DataType = SqlDbType.NVarChar,DataLength =255, AllowNull = true },


            //};
            //columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            //columnOptions.Store.Remove(StandardColumn.Properties);
            //columnOptions.Store.Remove(StandardColumn.Exception);


            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                //.WriteTo.MSSqlServer(
                //connectionString: builder.Configuration.GetConnectionString("GmaoConnection"),
                //sinkOptions: new MSSqlServerSinkOptions
                //{
                //    TableName = "Audit",
                //    AutoCreateSqlTable = true,
                //}, columnOptions: columnOptions
                //)
                .CreateLogger();



            MappingConfig.Config();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });



            // Build the application
            var app = builder.Build();
            app.UseCors("AllowAll");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
