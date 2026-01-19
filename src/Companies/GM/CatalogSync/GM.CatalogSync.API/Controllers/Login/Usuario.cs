// Clases DTO para login
public class LoginRequest
{
    public string usuario { get; set; }
    public string password { get; set; }
    public string empresaId { get; set; }
    public string agenciaId { get; set; }
}

public class LoginResponse
{
    public string response { get; set; }
    public string message { get; set; }
    public UsuarioInfo results { get; set; }
}

public class UsuarioInfo
{
    public string usuario { get; set; }
    public string empresaId { get; set; }
    public string agenciaId { get; set; }
    public string usuarioID { get; set; }
    public string nombreUsuario { get; set; }
    public string nombreAgencia { get; set; }
    public string urlImagen { get; set; }
    public string token { get; set; }
}

// Clase para configuración JWT
public class Jwt
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string Subject { get; set; }
}