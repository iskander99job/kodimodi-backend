using GameServer.Routes;
using GameServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

if (File.Exists(".env"))
    DotNetEnv.Env.NoClobber().Load(".env");

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var restApiUrl = builder.Configuration["RestApi:BaseUrl"];
builder.Services.AddHttpClient<MatchLoadout>(client =>
{
    if (!string.IsNullOrWhiteSpace(restApiUrl))
        client.BaseAddress = new Uri(restApiUrl.TrimEnd('/') + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(2);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

RoomRoute.Map(app);
LobbyRoute.Map(app);
GameRoute.Map(app);

await app.RunAsync();