using CarInsuranceSalesBot.Controllers;
using CarInsuranceSalesBot.Services;
using Microsoft.OpenApi.Models;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Car Insurance Bot API", Version = "v1" } ));

builder.Services.AddSingleton<ITelegramBotClient>(provider => 
    new TelegramBotClient(builder.Configuration["TelegramBot:Token"] ?? "7730339425:AAFhHJ6XR1eNSFyymcPajMiL6Oi44tkeYfs"));
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddSingleton<UserSessionManager>();
builder.Services.AddSingleton<GeminiService>();
builder.Services.AddSingleton<MindeeService>();
builder.Services.AddSingleton<InsuranceService>();

builder.Services.AddHostedService<TelegramBotHostedService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
