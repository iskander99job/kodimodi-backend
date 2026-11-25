using GameServer.Routes;
using GameServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

var envPath = Path.GetFullPath(".env");
if (!File.Exists(envPath))
{
    // Rider can launch the executable from bin/Debug/net10.0.
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
         directory != null;
         directory = directory.Parent)
    {
        if (!File.Exists(Path.Combine(directory.FullName, "GameServer.csproj")))
            continue;

        envPath = Path.Combine(directory.FullName, ".env");
        break;
    }
}

if (File.Exists(envPath))
    DotNetEnv.Env.NoClobber().Load(envPath);

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
Console.WriteLine($"RestApiUrl: {restApiUrl}");
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