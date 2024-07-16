using ConsumoAPIContagem.Extensions;
using ConsumoAPIContagem.Models;
using Microsoft.Extensions.Configuration;
using Flurl;
using Flurl.Http;
using Polly;
using Polly.Retry;
using Serilog.Core;
using System.Net;
using System.Text.Json;

namespace ConsumoAPIContagem.Clients;

public class APIContagemClient : IDisposable
{
    private Url? _loginUrl;
    private Url? _contagemUrl;
    private IConfiguration? _configuration;
    private Logger? _logger;
    private Token? _token;
    private AsyncRetryPolicy? _jwtPolicy;
    private JsonSerializerOptions? _serializerOptions;

    public bool IsAuthenticatedUsingToken
    {
        get => _token?.Authenticated ?? false;
    }

    public APIContagemClient(IConfiguration configuration,
        Logger logger)
    {
        _configuration = configuration;
        _logger = logger;

        string urlBase = _configuration.GetSection(
            "APIContagem_Access:UrlBase").Value!;
        _loginUrl = urlBase.AppendPathSegment("login");
        _contagemUrl = urlBase.AppendPathSegment("contador");

        _jwtPolicy = CreateAccessTokenPolicy();
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }

    public async Task Autenticar()
    {
        try
        {
            // Envio da requisição a fim de autenticar
            // e obter o token de acesso
            _token = await _loginUrl!.PostJsonAsync(
                new User()
                {
                    UserID = _configuration!.GetSection("APIContagem_Access:UserID").Value,
                    Password = _configuration.GetSection("APIContagem_Access:Password").Value
                }).ReceiveJson<Token>();
            _logger!.Information("Token JWT:" +
                Environment.NewLine +
                FormatJSONPayload<Token>(_token));
            _logger.Information("Payload do Access Token JWT:" +
                Environment.NewLine +
                FormatJSONPayload<PayloadAccessToken>(
                    Jose.JWT.Payload<PayloadAccessToken>(_token.AccessToken)));
        }
        catch
        {
            _token = null;
            _logger!.Error("Falha ao autenticar...");
        }
    }

    private string FormatJSONPayload<T>(T payload) =>
        JsonSerializer.Serialize(payload, _serializerOptions);

    private AsyncRetryPolicy CreateAccessTokenPolicy()
    {
        return Policy
            .HandleInner<FlurlHttpException>(
                ex => ex.StatusCode == (int)HttpStatusCode.Unauthorized)
            .RetryAsync(1, async (ex, retryCount, context) =>
            {
                var corAnterior = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Red;
                await Console.Out.WriteLineAsync(
                    Environment.NewLine + "Token expirado ou usuário sem permissão!");
                Console.ForegroundColor = corAnterior;

                Console.ForegroundColor = ConsoleColor.Green;
                await Console.Out.WriteLineAsync(
                    Environment.NewLine + "Execução de RetryPolicy..." +
                    Environment.NewLine);
                Console.ForegroundColor = corAnterior;

                await Autenticar();
                if (!(_token?.Authenticated ?? false))
                    throw new InvalidOperationException("Token inválido!");

                context["AccessToken"] = _token.AccessToken;
            });
    }

    public async Task ExibirResultadoContador()
    {
        var retorno = await _jwtPolicy!.ExecuteWithTokenAsync<ResultadoContador>(
            _token!, async (context) =>
        {
            var resultado = await _contagemUrl
                .WithOAuthBearerToken($"{context["AccessToken"]}")
                .GetJsonAsync<ResultadoContador>();
            return resultado;
        });
        _logger!.Information("Retorno da API de Contagem: " +
            Environment.NewLine +
            FormatJSONPayload<ResultadoContador>(retorno));
    }

    public void Dispose()
    {
        _loginUrl = null;
        _contagemUrl = null;
        _configuration = null;
        _logger = null;
        _token = null;
        _jwtPolicy = null;
        _serializerOptions = null;
    }
}