using OneCompetitions.Infrastructure;
using OneCompetitions.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSerilog(configuration => configuration.Enrich.FromLogContext().WriteTo.Console());
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
