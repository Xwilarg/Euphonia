using Euphonia.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Euphonia.API
{
    public class Program
    {
        public static void InitPath(WebsiteManager manager, string path)
        {
            if (!path.EndsWith('/') && !path.EndsWith('\\')) path += '/';
            manager.Endpoints.Add(path);

            if (!Directory.Exists($"{path}raw")) Directory.CreateDirectory($"{path}raw");
            if (!Directory.Exists($"{path}normalized")) Directory.CreateDirectory($"{path}normalized");
            if (!Directory.Exists($"{path}icon")) Directory.CreateDirectory($"{path}icon");
            if (!Directory.Exists($"{path}icon/playlist")) Directory.CreateDirectory($"{path}icon/playlist");
            if (!Directory.Exists($"{path}info.json")) File.WriteAllText($"{path}info.json", "{}");
        }

        public static void Main(string[] args)
        {
            var manager = new WebsiteManager();
            if (Directory.Exists("/euphonia/data"))
            {
                foreach (var dir in Directory.GetDirectories("/euphonia/data"))
                {
                    InitPath(manager, dir);
                }
            }

            var builder = WebApplication.CreateBuilder(args);
#if DEBUG
            builder.Logging.AddConsole();
#endif
            builder.Services.AddSingleton(manager);
            builder.Services.AddHttpClient();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("debug", p =>
                {
                    p.WithOrigins("http://localhost:5151").AllowAnyHeader();
                });
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var data = Encoding.UTF8.GetBytes("EffyIsLoveYouButPleaseINeedABetterPassword");
                var securityKey = new SymmetricSecurityKey(data);

#if DEBUG
                options.RequireHttpsMetadata = false;
#else
                options.RequireHttpsMetadata = true;
#endif

                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,

                    ValidateLifetime = true,

                    ValidateAudience = false,
                    ValidateIssuer = false,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseCors("debug");
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
