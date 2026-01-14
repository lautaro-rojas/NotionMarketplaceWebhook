using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotionWebhookService.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI: email service
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
// Background queue and processor
builder.Services.AddSingleton<NotionWebhookService.Services.IBackgroundTaskQueue, NotionWebhookService.Services.BackgroundTaskQueue>();
builder.Services.AddHostedService<NotionWebhookService.Services.QueuedHostedService>();

var app = builder.Build();

// Habilitar Swagger en todos los ambientes (dev y producción)
app.UseSwagger();
app.UseSwaggerUI();

// Middlewares
app.UseHttpsRedirection();
app.MapControllers();

app.Run();