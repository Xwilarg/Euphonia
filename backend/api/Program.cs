using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Euphonia.API
{
    public class Program
    {
        public static void InitPath()
        {

            if (!Directory.Exists($"/data/raw")) Directory.CreateDirectory($"/data/raw");
            if (!Directory.Exists($"/data/normalized")) Directory.CreateDirectory($"/data/normalized");
            if (!Directory.Exists($"/data/icon")) Directory.CreateDirectory($"/data/icon");
            if (!Directory.Exists($"/data/icon/playlist")) Directory.CreateDirectory($"/data/icon/playlist");
            if (!File.Exists($"/data/info.json")) File.WriteAllText($"/data/info.json", "{}");
            if (!File.Exists($"/data/credentials.json")) File.WriteAllText($"/data/credentials.json", Serialization.Serialize<EuphoniaCredentials>(new()));
            if (!File.Exists($"/data/metadata.json")) File.WriteAllText($"/data/metadata.json", Serialization.Serialize<EuphoniaMetadata>(new()));
        }

        public static void Main(string[] args)
        {
            InitPath();

            var builder = WebApplication.CreateBuilder(args);
#if DEBUG
            builder.Logging.AddConsole();
#endif
            builder.Services.AddSingleton<DownloaderManager>();
            builder.Services.AddSingleton<ExportManager>();
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
