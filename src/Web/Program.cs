using ChatSupportSystem.Application;
using ChatSupportSystem.Application.Common.Interfaces;
using ChatSupportSystem.Infrastructure;
using ChatSupportSystem.Web.Infrastructure;
using ChatSupportSystem.Web.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Chat Management API",
        Version = "v1",
        Description = "API for managing chat support sessions",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@example.com"
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Management API V1");
        c.RoutePrefix = string.Empty;
    });

    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<ChatSupportSystem.Infrastructure.Data.ChatSupportDbContextInitialiser>();
    await initializer.InitializeAsync();
    await initializer.SeedAsync();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseHealthChecks("/health");
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.MapEndpoints();

app.Run();