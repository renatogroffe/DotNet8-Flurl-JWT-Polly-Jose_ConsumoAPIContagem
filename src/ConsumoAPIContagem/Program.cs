using Microsoft.Extensions.Configuration;
using Serilog;
using ConsumoAPIContagem.Clients;

var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile($"appsettings.json");
var config = builder.Build();
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

logger.Information("***** Testes com JWT + Flurl + Polly (Retry Policy) *****");
logger.Information("A API consumida está em: " +
    Environment.NewLine +
    "https://github.com/renatogroffe/ASPNETCore8-WebApi-JWT-Swagger-Extensions-CustomClaims-HttpFiles_ContagemAcessos");

using var apiContagemClient = new APIContagemClient(config, logger);
await apiContagemClient.Autenticar();
while (true)
{
    await apiContagemClient.ExibirResultadoContador();
    logger.Information("Pressione qualquer tecla para continuar...");
    Console.ReadKey();
}