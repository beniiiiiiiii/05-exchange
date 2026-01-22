using Solution.WebAPI.Configurations;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.ConfigureDatabase()
       .LoadSettings()
       .UseSecurity()
       .UseIdentity()
       .ConfigureDI()
       .LoadEnvironmentVariables()
       //.UseScalarOpenAPI()
       //.UseSwashbuckleOpenAPI()
       .UseReDocOpenAPI();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseRouting();
app.UseSecurity();
app.MapControllers();
//app.UseScalarOpenAPI();
//app.UseSwashbuckleOpenAPI();
app.UseReDocOpenAPI();

await app.RunAsync();
