using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Solution.WebAPI.Transformers;

namespace Solution.WebAPI.Configurations;

public static class ConfigureScalarOpenAPI
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder UseScalarOpenAPI()
        {
            builder.Services.AddOpenApi(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public IApplicationBuilder UseScalarOpenAPI()
        {
            app.MapOpenApi();

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("MyApp API Documentation")
                       .WithTheme(ScalarTheme.Default)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                       .WithClassicLayout()
                       .ForceDarkMode()
                       .HideSearch()
                       .ShowOperationId()
                       .ExpandAllTags()
                       .SortTagsAlphabetically()
                       .SortOperationsByMethod();
            });

            return app;
        }
    }
}
