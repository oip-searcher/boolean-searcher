using BooleanSearcher.Options;
using BooleanSearcher.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<BooleanSearchOptions>()
    .Bind(builder.Configuration.GetSection("BooleanSearch"));

builder.Services.AddHostedService<BooleanSearchWorker>();

var app = builder.Build();

app.Run();