# 🔐 Fluxo Completo de JWT e Endpoints de Autenticação no CrudIo.Api

Este documento explica detalhadamente como o JWT (JSON Web Token) é implementado, configurado e utilizado no projeto CrudIo.Api, abrangendo ambos os fluxos de autenticação (usuário final e cliente API) e todos os endpoints relacionados.

## 🔄 Visão Geral do Fluxo de Autenticação

```mermaid
graph TD
    A[Requisição HTTP] --> B{Endpoint de Autenticação?}
    B -->|/auth/login| C[Fluxo de Usuário Final]
    B -->|/auth/client-token| D[Fluxo de Cliente API]
    B -->|/auth/refresh-token| E[Fluxo de Refresh Token]
    C --> F[Validação Email/Senha]
    D --> G[Validação Client-ID/API-Key]
    E --> H[Validação Refresh Token]
    F --> I[Geração de JWT de Usuário]
    G --> J[Geração de Par de Tokens (Access + Refresh)]
    H --> K[Validação + Geração de Novo Par]
    I --> L[Retorna Access Token de Usuário]
    J --> L
    K --> L
    L --> M[Cliente Armazena Token]
    M --> N[Requisição Protegida com Authorization: Bearer <token>]
    N --> O[Middleware de Validação JWT]
    O --> P{Token Válido?}
    P -->|Sim| Q[Endpoint Protegido Executado]
    P -->|Não| R[401 Unauthorized]
```

## 📋 Etapas Detalhadas

### 1. **Configuração do JWT (appsettings + Environment → JwtSettings)**

As configurações de JWT são carregadas de variáveis de ambiente com padrões definidos no código.

**Arquivo:** `src/Program.cs` (linhas 47-63)
```csharp
// JWT
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? "your-secret-key-change-in-production-32-chars-minimum-1234567890";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CrudIo.Api";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CrudIo.Client";
var jwtExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var exp) ? exp : 60;
var clientAccessTokenExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var clientExp) ? clientExp : 15;
var clientRefreshTokenExpirationDays = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS"), out var refreshExp) ? refreshExp : 30;

var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationMinutes = jwtExpirationMinutes,
    ClientAccessTokenExpirationMinutes = clientAccessTokenExpirationMinutes,
    ClientRefreshTokenExpirationDays = clientRefreshTokenExpirationDays
};

builder.Services.AddSingleton(jwtSettings); // Registrado como singleton
```

**Arquivo:** `src/CrudIo.Api/Common/Security/JwtSettings.cs`
```csharp
public class JwtSettings
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationMinutes { get; set; }           // Para usuários finais
    public int ClientAccessTokenExpirationMinutes { get; set; } // Para clientes API
    public int ClientRefreshTokenExpirationDays { get; set; }   // Para clientes API
}
```

**Valores Configurados (conforme arquivo .env atual):**
| Configuração | Valor Atual | Descrição |
|--------------|-------------|-----------|
| `JWT_SECRET` | Configurada no `.env` (valor seguro não divulgado) | Chave secreta para assinatura HMAC-SHA256 |
| `JWT_ISSUER` | `CrudIo.Api` | Emissor do token (iss claim) |
| `JWT_AUDIENCE` | `CrudIo.Client` | Audiência esperada (aud claim) |
| `JWT_EXPIRATION_MINUTES` | `3600` (60 horas) | Expiração do token de usuário em minutos |
| `CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES` | `15` | Expiração do access token de cliente em minutos |
| `CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS` | `30` | Expiração do refresh token de cliente em dias |

### 2. **Fluxo de Autenticação de Usuário Final (Email/Senha → JWT)**

**Endpoint:** `POST /auth/login`  
**Arquivo:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs`
```csharp
group.MapPost("/login", async (
    HttpContext context,
    ISender sender,
    [FromBody] LoginCommand command) =>
{
    try
    {
        var result = await sender.Send(command); // → LoginCommandHandler
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return ErrorResponse.Create(
            context,
            StatusCodes.Status401Unauthorized,
            "INVALID_CREDENTIALS",
            ex.Message);
    }
    // ... tratamento de exceções
});
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/Login/LoginCommand.cs`
```csharp
public sealed class LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/Login/LoginCommandHandler.cs`
```csharp
public async Task<LoginResponse> Handle(
    LoginCommand request,
    CancellationToken cancellationToken)
{
    // 1️⃣ BUSCA USUÁRIO POR EMAIL
    var user = await _dbContext.Users
        .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken)
        ?? throw new UnauthorizedAccessException("Invalid email or password.");

    // 2️⃣ VALIDA SENHA (com hash BCrypt)
    var isPasswordValid = _passwordService.Verify(
        request.Password,
        user.PasswordHash);

    if (!isPasswordValid)
        throw new UnauthorizedAccessException("Invalid email or password.");

    // 3️⃣ GERA JWT DE USUÁRIO
    var token = _jwtService.GenerateUserToken(
        user.Id,
        user.Email); // ← Email usado como nameidentifier claim

    var expiresAt = DateTime.UtcNow.AddMinutes(
        _jwtSettings.ExpirationMinutes);

    return new LoginResponse
    {
        Token = token,
        ExpiresIn = _jwtSettings.ExpirationMinutes * 60, // em segundos
        ExpiresAt = expiresAt
    };
}
```

**Arquivo:** `src/CrudIo.Api/Common/Security/JwtService.cs` (método GenerateUserToken)
```csharp
public string GenerateUserToken(Guid userId, string email)
{
    var claims = new[]
    {
        // Claim padrão para identificador de usuário (email)
        new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", email),
        // Claim personalizada para ID interno do usuário
        new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/guid", userId.ToString()),
        // Claim de papel/role (padrão: "User")
        new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "User")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Estrutura do JWT de Usuário (decodificado):**
```json
{
  "iss": "CrudIo.Api",
  "aud": "CrudIo.Client",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "godoyrw2@gmail.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/guid": "29e8101f-14ba-4148-8ea8-121f6cc57824",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "User",
  "exp": 1781908049, // timestamp UTC
  "nbf": 1781904449,
  "iat": 1781904449
}
```

### 3. **Fluxo de Autenticação de Cliente API (Client-ID/API-Key → Access+Refresh Tokens)**

**Endpoint:** `POST /auth/client-token`  
**Arquivo:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs`
```csharp
group.MapPost("/client-token", async (
    HttpContext context,
    ISender sender,
    [FromHeader(Name = "client-id")] string clientId,
    [FromHeader(Name = "client-api-key")] string clientApiKey) =>
{
    try
    {
        var result = await sender.Send(
            new ClientTokenCommand(clientId, clientApiKey)); // → ClientTokenCommandHandler
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return ErrorResponse.Create(
            context,
            StatusCodes.Status401Unauthorized,
            "INVALID_CLIENT_CREDENTIALS",
            "Invalid client credentials.");
    }
    // ... tratamento de exceções
});
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/ClientToken/ClientTokenCommand.cs`
```csharp
public record ClientTokenCommand(
    string ClientId,
    string ClientApiKey) : IRequest<ClientTokenResponse>;

public record ClientTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt);
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/ClientToken/ClientTokenCommandHandler.cs`
```csharp
public async Task<ClientTokenResponse> Handle(
    ClientTokenCommand request,
    CancellationToken cancellationToken)
{
    // Validação básica
    if (string.IsNullOrWhiteSpace(request.ClientId) ||
        string.IsNullOrWhiteSpace(request.ClientApiKey))
    {
        throw new UnauthorizedAccessException("Invalid client credentials.");
    }

    // 1️⃣ BUSCA CLIENTE NO BANCO (populado via ApiClientSeeder do .env)
    var apiClient = await _dbContext.ApiClients
        .FirstOrDefaultAsync(x =>
            x.ClientId == request.ClientId &&
            x.IsActive &&
            x.RevokedAt == null,
            cancellationToken)
        ?? throw new UnauthorizedAccessException("Invalid client credentials.");

    // 2️⃣ VALIDA CHAVE DE API (comparação com hash BCrypt)
    var isApiKeyValid = _passwordService.Verify(
        request.ClientApiKey,
        apiClient.ApiKeyHash);

    if (!isApiKeyValid)
        throw new UnauthorizedAccessException("Invalid client credentials.");

    // 3️⃣ GERA REFRESH TOKEN (aleatório, não JWT)
    var refreshToken = RefreshTokenService.Generate();
    var refreshExpiresAt = DateTime.UtcNow.AddDays(
        _jwtSettings.ClientRefreshTokenExpirationDays);

    // 4️⃣ ARMAZENA REFRESH TOKEN (hash no banco)
    _dbContext.ClientRefreshTokens.Add(new ClientRefreshToken
    {
        Id = Guid.NewGuid(),
        ApiClientId = apiClient.Id,
        TokenHash = RefreshTokenService.Sha256(refreshToken), // Hash SHA-256
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = refreshExpiresAt
    });

    await _dbContext.SaveChangesAsync(cancellationToken);

    // 5️⃣ GERA ACCESS TOKEN DE CLIENTE (JWT)
    var accessToken = _jwtService.GenerateClientToken(
        apiClient.Id,
        apiClient.ClientId);

    var expiresAt = DateTime.UtcNow.AddMinutes(
        _jwtSettings.ClientAccessTokenExpirationMinutes);

    return new ClientTokenResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        TokenType = "Bearer",
        ExpiresIn = _jwtSettings.ClientAccessTokenExpirationMinutes * 60, // em segundos
        ExpiresAt = expiresAt,
        RefreshExpiresAt = refreshExpiresAt
    };
}
```

**Arquivo:** `src/CrudIo.Api/Common/Security/JwtService.cs` (método GenerateClientToken)
```csharp
public string GenerateClientToken(Guid apiClientId, string clientId)
{
    var claims = new[]
    {
        // Claim de identificador de cliente (do banco)
        new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", clientId),
        // Claim personalizada para ID interno do cliente
        new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/guid", apiClientId.ToString()),
        // Claim de papel/role fixo para clientes API
        new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Client")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ClientAccessTokenExpirationMinutes),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Estrutura do JWT de Cliente API (decodificado):**
```json
{
  "iss": "CrudIo.Api",
  "aud": "CrudIo.Client",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "crudio-client",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/guid": "c10b4c34-708e-426e-af4e-45ebea98b249",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Client",
  "exp": 1781907657, // timestamp UTC (15 min a partir de agora)
  "nbf": 1781904057,
  "iat": 1781904057
}
```

### 4. **Fluxo de Refresh Token de Cliente API (Refresh Token → Novos Access+Refresh Tokens)**

**Endpoint:** `POST /auth/refresh-token`  
**Arquivo:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs`
```csharp
group.MapPost("/refresh-token", async (
    HttpContext context,
    ISender sender,
    [FromBody] RefreshTokenCommand command) =>
{
    try
    {
        var result = await sender.Send(command); // → RefreshTokenCommandHandler
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return ErrorResponse.Create(
            context,
            StatusCodes.Status401Unauthorized,
            "INVALID_REFRESH_TOKEN",
            "Invalid refresh token.");
    }
    // ... tratamento de exceções
});
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/RefreshToken/RefreshTokenCommand.cs`
```csharp
public sealed class RefreshTokenCommand : IRequest<ClientTokenResponse>
{
    public string RefreshToken { get; init; } = string.Empty;
}
```

**Arquivo:** `src/CrudIo.Api/Features/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
```csharp
public async Task<ClientTokenResponse> Handle(
    RefreshTokenCommand request,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
        throw new UnauthorizedAccessException("Invalid refresh token.");

    // 1️⃣ BUSCA REFRESH TOKEN NO BANCO (por hash)
    var storedToken = await _dbContext.ClientRefreshTokens
        .FirstOrDefaultAsync(x =>
            x.TokenHash == RefreshTokenService.Sha256(request.RefreshToken) &&
            x.ExpiresAt > DateTime.UtcNow &&    // Não expirado
            x.RevokedAt == null,                // Não revogado
            cancellationToken)
        ?? throw new UnauthorizedAccessException("Invalid refresh token.");

    // 2️⃣ BUSCA CLIENTE ASSOCIADO
    var apiClient = await _dbContext.ApiClients
        .FirstOrDefaultAsync(x => x.Id == storedToken.ApiClientId, cancellationToken)
        ?? throw new UnauthorizedAccessException("Invalid refresh token.");

    // 3️⃣ REVOGA REFRESH TOKEN ANTIGO (rotação segura)
    storedToken.RevokedAt = DateTime.UtcNow;
    storedToken.UsedAt = DateTime.UtcNow;

    // 4️⃣ GERA NOVO REFRESH TOKEN
    var newRefreshToken = RefreshTokenService.Generate();
    var newRefreshExpiresAt = DateTime.UtcNow.AddDays(
        _jwtSettings.ClientRefreshTokenExpirationDays);

    // 5️⃣ ARMAZENA NOVO REFRESH TOKEN
    _dbContext.ClientRefreshTokens.Add(new ClientRefreshToken
    {
        Id = Guid.NewGuid(),
        ApiClientId = apiClient.Id,
        TokenHash = RefreshTokenService.Sha256(newRefreshToken),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = newRefreshExpiresAt
    });

    await _dbContext.SaveChangesAsync(cancellationToken);

    // 6️⃣ GERA NOVO ACCESS TOKEN (mesmo processo do client-token)
    var accessToken = _jwtService.GenerateClientToken(
        apiClient.Id,
        apiClient.ClientId);

    var expiresAt = DateTime.UtcNow.AddMinutes(
        _jwtSettings.ClientAccessTokenExpirationMinutes);

    return new ClientTokenResponse
    {
        AccessToken = accessToken,
        RefreshToken = newRefreshToken,
        TokenType = "Bearer",
        ExpiresIn = _jwtSettings.ClientAccessTokenExpirationMinutes * 60,
        ExpiresAt = expiresAt,
        RefreshExpiresAt = newRefreshExpiresAt
    };
}
```

### 5. **Validação de JWT em Endpoints Protegidos (Middleware)**

Todas as rotas em `/users/*` e outros endpoints protegidos usam o middleware de autenticação JWT configurado em `Program.cs`.

**Arquivo:** `src/Program.cs` (linhas 69-88)
```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Rejeita imediatamente se expirado
        };
    });

builder.Services.AddAuthorization();

// ...

app.UseAuthentication();
app.UseAuthorization();
```

**Como funciona na prática:**
1. Quando uma requisição chega com `Authorization: Bearer <token>`
2. O middleware `JwtBearerHandler` intercepta a requisição
3. Ele:
   - Extrai o token do header
   - Valida a assinatura usando a chave secreta
   - Verifica o `iss` (issuer) contra `JwtSettings.Issuer`
   - Verifica o `aud` (audience) contra `JwtSettings.Audience`
   - Verifica se não está expirado (comparando `exp` com utc agora)
   - Se tudo válido, cria um `ClaimsPrincipal` com as claims do token
   - Define `HttpContext.User` para esse principal
4. Se a validação falhar em qualquer etapa, retorna `401 Unauthorized` imediatamente
5. Se bem-sucedido, a requisição continua para o endpoint específico

**Arquivo:** `src/CrudIo.Api/Features/Users/UsersEndpoints.cs` (exemplo de proteção)
```csharp
var group = app.MapGroup("/users")
    .WithTags("Users")
    .RequireAuthorization(); // ← Isso ativa o middleware JWT
```

## 🔐 **Benefícios de Segurança desta Implementação**

1. **Assinatura Forte (HMAC-SHA256)**
   - Tokens são assinados com uma chave secreta de 32+ caracteres
   - Qualquer modificação no token invalida a assinatura

2. **Validação Rigorosa de Claims**
   - `iss` (issuer) deve ser exatamente `CrudIo.Api`
   - `aud` (audience) deve ser exatamente `CrudIo.Client`
   - `exp` (expiration) é validada com `ClockSkew = TimeSpan.Zero`

3. **Dois Fluxos Distintos**
   - **Usuários finais:** Autenticação por email/senha → JWT de curta duração (60 min)
   - **Clientes API:** Autenticação por client-id/api-key → Par de tokens (access: 15 min, refresh: 30 dias)

4. **Refresh Token Seguro (Clientes API apenas)**
   - Refresh tokens são armazenados como **hash SHA-256** no banco (nunca em texto plano)
   - Rotação automática: refresh token usado é revogado e um novo é gerado
   - Validação de expiração e revogação em cada uso

5. **Proteção contra Replay**
   - Tokens têm vida curta (especialmente access tokens de cliente: 15 min)
   - JTI (JWT ID) não é usado, mas a combinação de vida curta + refresh token rotation fornece segurança adequada

6. **Isolamento de Papéis (Roles)**
   - Usuários finais recebem claim `role: "User"`
   - Clientes API recebem claim `role: "Client"`
   - Endpoints podem aplicar políticas de autorização baseadas em role (embora atualmente não implementado, a infraestrutura está pronta)

## 📋 **Endpoints de Autenticação Especificados**

### **1. POST /auth/login** (Usuário Final)
**Autentica:** Email + Senha  
**Retorna:** JWT de acesso para usuário final  
**Duração:** 60 minutos (configurável via `JWT_EXPIRATION_MINUTES`)  
**Headers:** `Content-Type: application/json`  
**Request Body:**
```json
{
  "email": "usuario@exemplo.com",
  "senha": "senha123"
}
```
**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 216000,
  "expiresAt": "2026-06-23T12:00:00Z"
}
```
**Erros Comuns:**
- `401`: `INVALID_CREDENTIALS` - Email ou senha incorretos
- `400`: Payload inválido ou malformado
- `500`: Erro interno do servidor

### **2. POST /auth/client-token** (Cliente API)
**Autentica:** Client-ID + Client-API-Key (headers)  
**Retorna:** Par de tokens (Access Token JWT + Refresh Token aleatório)  
**Duração Access Token:** 15 minutos (configurável via `CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES`)  
**Duração Refresh Token:** 30 dias (configurável via `CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS`)  
**Headers:** 
- `client-id: <valor-do-.env>`
- `client-api-key: <valor-do-.env>`
**Request Body:** Nenhum (todos dados nos headers)  
**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "zOGphOwIQCWHYf28wrmSyzhlfqOGvdQ8i2kyu_3p20qJIRy8tstgwi0skMwSyL_tby1nz8BTNkW9pfPFWXe4AQ",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "expiresAt": "2026-06-20T12:15:00Z",
  "refreshExpiresAt": "2026-07-20T12:00:00Z"
}
```
**Erros Comuns:**
- `401`: `INVALID_CLIENT_CREDENTIALS` - Credenciais de cliente inválidas ou inativas
- `500`: Erro interno do servidor

### **3. POST /auth/refresh-token** (Cliente API)
**Autentica:** Refresh Token (body)  
**Retorna:** Novos par de tokens (Access Token JWT + novo Refresh Token)  
**Duração Access Token:** 15 minutos (mesmo que acima)  
**Duração Refresh Token:** 30 dias (mesmo que acima, a partir de agora)  
**Headers:** `Content-Type: application/json`  
**Request Body:**
```json
{
  "refreshToken": "zOGphOwIQCWHYf28wrmSyzhlfqOGvdQ8i2kyu_3p20qJIRy8tstgwi0skMwSyL_tby1nz8BTNkW9pfPFWXe4AQ"
}
```
**Response (200 OK):** Mesmo formato do endpoint `/client-token` acima  
**Erros Comuns:**
- `401`: `INVALID_REFRESH_TOKEN` - Token inválido, expirado ou revogado
- `400`: Payload inválido ou malformado
- `500`: Erro interno do servidor

## 📝 **Formato de Erro Padronizado**

Todos os endpoints de autenticação retornam erros no formato:
```json
{
  "code": "Código de erro específico",
  "message": "Mensagem legível em português ou inglês",
  "statusCode": Código HTTP (400, 401, 500, etc),
  "traceId": "Identificador único de rastreamento (ex: 0HNME94OCP2B7:00000001)"
}
```

**Exemplos:**
```json
// 401 - Credenciais inválidas
{
  "code": "INVALID_CREDENTIALS",
  "message": "Invalid email or password.",
  "statusCode": 401,
  "traceId": "0HNME94OCP2B7:00000001"
}

// 401 - Token expirado
{
  "code": "TOKEN_EXPIRED",
  "message": "The token has expired.",
  "statusCode": 401,
  "traceId": "0HNME94OCP2B7:00000002"
}

// 400 - Payload inválido
{
  "code": "INVALID_PAYLOAD",
  "message": "The request payload is invalid.",
  "statusCode": 400,
  "traceId": "0HNME94OCP2B7:00000003"
}
```

## 🛠️ **Como Testar os Fluxos de Autenticação**

### **Testando Login de Usuário Final**
```bash
# Primeiro, garantir que existe um usuário (pode ser criado via client token)
curl -X POST http://localhost:5051/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"

# Usar o access token acima para criar um usuário (se nenhum existir)
# Ou usar credenciais pré-existentes no banco
curl -X POST http://localhost:5051/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "godoyrw2@gmail.com",
    "password": "secret123"
  }'
```

### **Testando Autenticação de Cliente API**
```bash
curl -X POST http://localhost:5051/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
```

### **Testando Refresh Token**
```bash
# 1. Obter par de tokens iniciais
ACCESS_TOKEN=$(curl -s -X POST http://localhost:5051/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2" | jq -r '.accessToken')

REFRESH_TOKEN=$(curl -s -X POST http://localhost:5051/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2" | jq -r '.refreshToken')

# 2. Usar access token em endpoint protegido
curl -H "Authorization: Bearer $ACCESS_TOKEN" http://localhost:5051/users

# 3. Após 15 minutos (quando access token expira), usar refresh token
curl -X POST http://localhost:5051/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
```

### **Testando Endpoint Protegido com Token de Usuário**
```bash
# Obtener token de usuario
USER_TOKEN=$(curl -s -X POST http://localhost:5051/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "godoyrw2@gmail.com",
    "password": "secret123"
  }' | jq -r '.token')

# Usar em endpoint protegido
curl -H "Authorization: Bearer $USER_TOKEN" http://localhost:5051/users
curl -H "Authorization: Bearer $USER_TOKEN" http://localhost:5051/users/29e8101f-14ba-4148-8ea8-121f6cc57824
```

## 🚨 **Solução de Problemas Comuns**

### **Problema: `401 Unauthorized` em `/auth/login`**
**Causas possíveis:**
1. **Credenciais incorretas**  
   → Verifique email e senha exatamente como cadastrados
   
2. **Usuário não existe**  
   → Primeiro crie um usuário via client token ou diretamente no banco
   
3. **Senha não está hasheada corretamente**  
   → Verifique se o `PasswordService` está funcionando (usa BCrypt)

### **Problema: `401 Unauthorized` em `/auth/client-token`**
**Causas possíveis:**
1. **Headers ausentes ou incorretos**  
   → Verifique se está enviando exatamente:
   - `client-id: crudio-client`
   - `client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2`
   
2. **Seeder não executou**  
   → Verifique logs de inicialização para "ApiClientSeeder"
   
3. **Cliente não existe ou está inativo no banco**  
   → Execute SQL: `SELECT * FROM api_clients WHERE client_id = 'crudio-client';`
   
4. **Chave de API não bate com o hash armazenado**  
   → O seeder hashea a chave do .env - verifique se não houve alteração manual

### **Problema: `401 Unauthorized` em endpoints protegidos (apesar de token válido)**
**Causas possíveis:**
1. **Token expirado**  
   → Verifique o campo `exp` no token (use https://jwt.io para decodificar)
   
2. **Issuer/Audience incorretos**  
   → O token deve ter `iss: "CrudIo.Api"` e `aud: "CrudIo.Client"`
   
3. **Assinatura inválida**  
   → O token foi modificado ou assinado com chave diferente
   
4. **Middleware não está configurado**  
   → Verifique se `app.UseAuthentication(); app.UseAuthorization();` está em Program.cs

### **Problema: Refresh token não funciona**
**Causas possíveis:**
1. **Refresh token expirado**  
   → Validade padrão: 30 dias a partir da geração
   
2. **Refresh token já foi usado (rotação)**  
   → Após uso, o token antigo é revogado - só o novo funciona
   
3. **Refresh token revogado manualmente**  
   → Verifique se `revoked_at` não está preenchido na tabela `client_refresh_tokens`
   
4. **Associacao ao cliente quebrada**  
   → Verifique se o `api_client_id` do refresh token aponta para um cliente ativo

### **Problema: Tokens parecem válidos mas são rejeitados por assinatura**
**Causas possíveis:**
1. **Chave secreta alterada entre emissão e validação**  
   → Se você alterou `JWT_SECRET` no .env e reiniciou, tokens antigos são invalidados
   
2. **Ambientes com chaves diferentes**  
   → Desenvolvimento, staging e produção devem ter segredos diferentes
   
3. **Token gerado em outro serviço**  
   → Confirme que o token foi gerado por esta instância específica da API

## 📂 **Referências de Código Diretas**

- **Configuração JWT:** `src/Program.cs` (linhas 47-63)
- **Classe de Configuração:** `src/CrudIo.Api/Common/Security/JwtSettings.cs`
- **Serviço JWT (Geração/Validação):** `src/CrudIo.Api/Common/Security/JwtService.cs`
- **Endpoint de Login:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs` (linhas 87-124)
- **Handler de Login:** `src/CrudIo.Api/Features/Auth/Login/LoginCommandHandler.cs`
- **Endpoint de Client Token:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs` (linhas 19-52)
- **Handler de Client Token:** `src/CrudIo.Api/Features/Auth/ClientToken/ClientTokenCommandHandler.cs`
- **Endpoint de Refresh Token:** `src/CrudIo.Api/Features/Auth/AuthEndpoints.cs` (linhas 54-85)
- **Handler de Refresh Token:** `src/CrudIo.Api/Features/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
- **Serviço de Senha (Hash/Verify):** `src/CrudIo.Api/Common/Security/PasswordService.cs`
- **Serviço de Refresh Token (Geracao):** `src/CrudIo.Api/Common/Security/RefreshTokenService.cs`
- **Middleware de Autenticação:** Configurado em `src/Program.cs` (linhas 69-90)
- **Entidades Relacionadas:**
  - `src/CrudIo.Api/Data/Entities/User.cs`
  - `src/CrudIo.Api/Data/Entities/ApiClient.cs`
  - `src/CrudIo.Api/Data/Entities/ClientRefreshToken.cs`

## ✅ **Resumo para Desenvolvedores**

> **"O JWT no CrudIo.Api fornece dois fluxos de autenticação com segurança em camadas."**
> 
> 1. 🔑 **Fluxo de Usuário Final:** Email/senha → JWT de usuário (60 min)  
>    - Ideal para aplicações interativas (web, mobile)
>    - Token curto reduz risco de roubo
>   
> 2. 🔐 **Fluxo de Cliente API:** Client-ID/API-Key → Par de tokens (Access: 15 min, Refresh: 30 dias)  
>    - Ideal para serviço-a-serviço e integrações automatizadas
>    - Refresh token rotation limita janela de ataque
>   
> 3. 🛡️ **Validação Rigorosa:** Todo token passou por:
>    - Verificação de assinatura HMAC-SHA256
>    - Confirmação de issuer e audience exatos
>    - Checagem de expiração com tolerância zero
>    - (Para refresh tokens) Verificação de hash SHA-256 no banco + não revogado
>   
> 4. 🔄 **Infraestrutura Pronta para Expansão:**
>    - Sistema de claims flexível (fácil adicionar novos tipos)
>    - Suporte inerente a políticas de role-based access control
>    - Logging de eventos de auth disponível via MongoLogger
> 
> **Para modificar comportamento:**
> - Alterar tempos de expiração: Ajustar variáveis de ambiente `.env`
> - Mudar chave de assinatura: Alterar `JWT_SECRET` no .env + reiniciar (invalida todos os tokens existentes)
> - Adicionar novos claims: Modificar os métodos `GenerateUserToken` e `GenerateClientToken` em `JwtService.cs`
> - Implementar autorização por role: Usar `[Authorize(Roles = "Admin")]` nos endpoints (requere adicionar claims de ruolo na geração)

Este implementação fornece autenticação segura, flexível e pronta para produção, seguindo as melhores práticas da indústria para aplicações .NET modernos.