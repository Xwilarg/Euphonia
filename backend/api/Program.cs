using Euphonia.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Euphonia.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
#if DEBUG
            builder.Logging.AddConsole();
#endif
            builder.Services.AddSingleton<WebsiteManager>();
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
