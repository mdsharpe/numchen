using Numchen.Api.Features.Game;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddGameFeature();

var app = builder.Build();

app.MapGameFeature();

app.Run();
