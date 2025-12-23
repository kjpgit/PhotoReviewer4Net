// Photo Reviewer 4Net (C) 2025 Karl Pickett

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using photo_reviewer_4net;


// This is a special CLI mode that just prints out the files for a certain
// category/rating, then exits.
if (args.Length >= 1 && (args[0] == "--println" || args[0] == "--print0")) {
    QueryService.PrintFiles(args);
    Environment.Exit(0);
}

var options = new ServerOptions(args);
var apiService = new APIService(options);
apiService.Init();


// All checks ok, now start ASP.NET web server
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
        WebRootPath = options.UseDevWebRoot ?
            "wwwroot" : Path.Join(AppContext.BaseDirectory, "wwwroot"),
        });

//Console.WriteLine($"ContentRoot Path: {builder.Environment.ContentRootPath}");
//Console.WriteLine($"WebRootPath: {builder.Environment.WebRootPath}");

// As a user-facing CLI tool, we want precise control over logging, to avoid clutter
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
if (options.VerboseRequestLogging) {
    builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Information);
}

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => {
    options.SerializerOptions.PropertyNamingPolicy = null; // Stop the camelCase
    options.SerializerOptions.TypeInfoResolverChain.Add(MyApiJsonContext.Default); // Json source generator
});

builder.Services.AddSingleton<APIService>(apiService);
builder.Services.AddSingleton<IHostLifetime, NoopConsoleLifetime>();
builder.Services.AddSingleton<ILoggerProvider, MinimalConsoleLoggerProvider>();
if (options.IsDocker()) {
    Console.WriteLine("Docker detected, using default HTTP settings");
} else {
    builder.WebHost.UseUrls([options.GetListenUrl()]);
}


var app = builder.Build();
app.UseDefaultFiles();  // map / to index.html
app.UseStaticFiles();   // serve content in wwwroot/
app.UseRouting();
//app.UseCors();        // CORS not required, UI is same origin
APIController.MapEndpoints(app);

app.Lifetime.ApplicationStarted.Register(() => {
    var server = app.Services.GetRequiredService<IServer>();
    var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
    if (serverAddressesFeature != null) {
        foreach (var address in serverAddressesFeature.Addresses) {
            Console.WriteLine($"\nNow listening for requests on: {address}");
            Console.WriteLine($"(Press ctrl-c to stop the server)\n");
        }
    }
    });

try {
    app.Run();
} catch (Exception e) {
    if (e.ContainsException<Microsoft.AspNetCore.Connections.AddressInUseException>()) {
        Console.WriteLine($"Error: The requested port ({options.ListenPort}) is already in use.  Try another port.");
    } else {
        throw;
    }
}
