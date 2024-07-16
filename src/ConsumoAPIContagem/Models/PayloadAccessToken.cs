namespace ConsumoAPIContagem.Models;

public class PayloadAccessToken
{
    public string[]? Unique_name { get; set; }
    public string? Jti { get; set; }
    public int Nbf { get; set; }
    public int Exp { get; set; }
    public int Iat { get; set; }
    public string? Iss { get; set; }
    public string? Aud { get; set; }
    public bool Success { get; set; }
    public string? Token_idp { get; set; }
    public int Year_month { get; set; }
    public Information? Info { get; set; }
}

public class Information
{
    public string? Author { get; set; }
    public string? Blog { get; set; }
    public string? GitHub { get; set; }
    public int Ano { get; set; }
}