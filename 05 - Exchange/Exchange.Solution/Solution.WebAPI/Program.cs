using Solution.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.ConfigureDatabase()
       .LoadSettings()
       .UseSecurity()
       .UseIdentity()
       .ConfigureDI()
       .LoadEnvironmentVariables()
       .UseScalarOpenAPI();
       //.UseSwashbuckleOpenAPI()
       //.UseReDocOpenAPI();

var app = builder.Build();

// Seed roles
await app.Services.SeedRolesAsync();

app.UseHttpsRedirection();
app.UseRouting();
app.UseSecurity();
app.MapControllers();
app.UseScalarOpenAPI();
//app.UseSwashbuckleOpenAPI();
//app.UseReDocOpenAPI();

await app.RunAsync();
