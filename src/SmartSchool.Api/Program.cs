using SmartSchool.Modules.Identity.Infrastructure;
using SmartSchool.Modules.Identity.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddIdentityPresentation();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
