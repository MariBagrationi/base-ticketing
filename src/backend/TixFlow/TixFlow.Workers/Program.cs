using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var host = builder.Build();
host.Run();
