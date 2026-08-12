using Anthropic.SDK;
using TalkBuddy.API.Config;
using TalkBuddy.API.Repositories;
using TalkBuddy.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind strongly-typed config
builder.Services.Configure<AppConfig>(builder.Configuration);

// Anthropic SDK client
builder.Services.AddSingleton(new AnthropicClient(
    builder.Configuration["AnthropicApiKey"]!
));

// Application services
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<ChatService>();

// MVC controllers
builder.Services.AddControllers();

// CORS — allowed origins come from config (AllowedOrigins:0, AllowedOrigins:1, ...)
// so the same image works against the local dev server and the deployed frontend.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Global exception handler — returns { "detail": "..." } with HTTP 500
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = feature?.Error?.Message ?? "An unexpected error occurred.";
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature?.Error, "Unhandled exception: {Message}", message);
        await ctx.Response.WriteAsJsonAsync(new { detail = message });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
