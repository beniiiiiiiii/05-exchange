using System.Reflection;

namespace Solution.WebAPI.Configurations;

public static class ConfigureRedocOpenAPI
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder UseReDocOpenAPI()
        {
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenApi(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "v11",
                    Title = "WeatherForecast API",
                    Description = "An ASP.NET Core Web API for managing WeatherForecast items",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Example Contact",
                        Url = new Uri("https://example.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://example.com/license")
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token.",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer",
                });

                options.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
            });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public IApplicationBuilder UseReDocOpenAPI()
        {
            app.MapOpenApi();

            app.UseSwagger(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            });

            app.UseReDoc(options =>
            {
                options.RoutePrefix = "redoc";
                options.SpecUrl = "/openapi/v1.json";
                options.DocumentTitle = "Weather Forecast API Documentation";
            }); ;

            return app;
        }
    }
}
