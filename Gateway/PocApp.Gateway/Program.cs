var builder = WebApplication.CreateBuilder(args);

// YARP picks up routes/clusters from appsettings.json -> "ReverseProxy" section.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/_gateway/health", () => Results.Ok(new { status = "ok", service = "Gateway" }));

app.MapReverseProxy();

app.Run();
