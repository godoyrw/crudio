# 📊 Resumo do Projeto **CrudIo.Api**

## 🎯 **O que é?**
Uma **API RESTful em .NET 10** para gerenciar usuários, seguindo **Vertical Slice Architecture**. Projeto cliente para a plataforma Crud.io com integração completa a infraestrutura em containers (PostgreSQL, MongoDB, Redis).

---

## 🏗️ **Arquitetura & Stack**

| Camada | Tecnologia |
|--------|-----------|
| **Framework** | ASP.NET Core 10 (Minimal APIs) |
| **Banco Relacional** | PostgreSQL 18 |
| **Banco NoSQL** | MongoDB 8 (logs) |
| **Cache** | Redis 8 |
| **Proxy/Load Balancer** | Nginx |
| **CQRS & Validação** | MediatR + FluentValidation |
| **ORM** | Entity Framework Core 10 |
| **Documentação** | OpenAPI (Swagger) |

---

## 📁 **Estrutura do Projeto**

```
src/
├── CrudIo.Api/
│   ├── Features/
│   │   ├── Auth/
│   │   │   ├── Login/                 (POST /api/auth/login)
│   │   │   ├── RefreshToken/          (POST /api/auth/refresh-token)
│   │   │   └── ClientToken/           (POST /api/auth/client-token)
│   │   └── Users/
│   │       ├── CreateUser/            (POST /api/users)
│   │       ├── GetUser/               (GET /api/users/{id})
│   │       ├── UpdateUser/            (PUT /api/users/{id})
│   │       ├── DeleteUser/            (DELETE /api/users/{id})
│   │       ├── ListUsers/             (GET /api/users?page=1&pageSize=10)
│   │       └── UsersEndpoints.cs      (Mapper de rotas)
│   │
│   ├── Data/
│   │   ├── AppDbContext.cs            (EF Core DbContext)
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── ApiClient.cs
│   │   │   └── ClientRefreshToken.cs
│   │   └── EntityConfigurations/
│   │       ├── UserConfiguration.cs
│   │       ├── ApiClientConfiguration.cs
│   │       └── ClientRefreshTokenConfiguration.cs
│   │
│   ├── Common/
│   │   ├── Behaviors/                 (Pipelines MediatR)
│   │   │   └── ValidationBehavior.cs
│   │   ├── Middleware/
│   │   ├── Security/
│   │   │   ├── JwtService.cs
│   │   │   ├── PasswordService.cs
│   │   │   ├── RefreshTokenService.cs
│   │   │   └── ApiClientSeeder.cs
│   │   ├── Validation/
│   │   ├── Pagination/
│   │   └── HealthChecks/
│   │
│   ├── Cache/
│   │   ├── RedisCacheService.cs
│   │   └── CacheKeys.cs
│   │
│   ├── Logging/
│   │   ├── MongoLogger.cs
│   │   └── LogEntry.cs
│   │
│   ├── Program.cs                     (Dependency Injection & Middlewares)
│   └── appsettings.json
│
├── Migrations/                        (EF Core Migrations)
└── Properties/
```

---

## 🔌 **Dependências Principais (NuGet)**

```xml
<PackageReference Include="FluentValidation" Version="11.11.0" />
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="BCrypt.Net-Next" Version="4.2.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
<PackageReference Include="StackExchange.Redis" Version="3.0.0" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
```

---

## 🚀 **Padrões Implementados**

### **1. Vertical Slice Architecture**
- Organização por **feature** (Users, Auth, etc), não por camadas
- Cada operação é independente
- Alta coesão, baixo acoplamento

### **2. CQRS (Command Query Responsibility Segregation)**
- **Commands**: `CreateUserCommand`, `UpdateUserCommand`, `DeleteUserCommand`, `LoginCommand`, `ClientTokenCommand`, `RefreshTokenCommand`
- **Queries**: `GetUserQuery`, `ListUsersQuery`
- Handlers via MediatR

### **3. Minimal APIs (ASP.NET Core)**
```csharp
group.MapPost("/", async (ISender sender, CreateUserCommand command) => ...)
group.MapGet("/{id:guid}", async (ISender sender, Guid id) => ...)
```

### **4. Tratamento de Erros Robusto**
- `ValidationException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 409 Conflict
- Respostas estruturadas com código, mensagem e traceId

### **5. Validação com FluentValidation**
- Pipeline behavior automático
- Validadores por feature

---

## 🔐 **Autenticação JWT**

A API usa autenticação por **JWT Bearer Token** com dois fluxos distintos:

### **Fluxo de Usuário Final**
1. Autentique em `POST /api/auth/login` usando `email` e `password`.
2. Guarde o campo `token` retornado.
3. Envie o token nas rotas protegidas usando o header:
   ```http
   Authorization: Bearer <token>
   ```

### **Fluxo de Cliente API (Service-to-Service)**
1. Autentique em `POST /api/auth/client-token` usando os headers:
   - `client-id`: Seu ID de cliente
   - `client-api-key`: Sua chave de API
2. Guarde os campos `accessToken` e `refreshToken` retornados.
3. Use o `accessToken` nas rotas protegidas da mesma forma que o token de usuário.
4. Quando o accessToken expirar, use o `refreshToken` em `POST /api/auth/refresh-token` para obter um novo par de tokens.

> **Observação:** Como não existe cadastro público de usuários, o primeiro usuário deve ser provisionado de forma administrativa (seed, migration/manual no banco ou outro processo interno). Depois disso, usuários autenticados podem criar novos usuários por `POST /api/users`.

### Configuração JWT

As configurações são lidas por variáveis de ambiente, com valores configurados atualmente no arquivo `.env`:

| Variável | Descrição | Valor Atual |
|----------|-----------|-------------|
| `JWT_SECRET` | Chave usada para assinar o token | Configurada no `.env` (valor seguro não divulgado) |
| `JWT_ISSUER` | Emissor esperado do token | `CrudIo.Api` |
| `JWT_AUDIENCE` | Audiência esperada do token | `CrudIo.Client` |
| `JWT_EXPIRATION_MINUTES` | Tempo de expiração do access token de usuário em minutos | `3600` (60 horas) |
| `CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES` | Tempo de expiração do access token de cliente em minutos | `15` |
| `CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS` | Tempo de expiração do refresh token de cliente em dias | `30` |

Em produção, configure obrigatoriamente um `JWT_SECRET` forte e privado.

### Exemplo de Uso com cURL

**Login de Usuário:**
```bash
curl -X POST http://localhost:5051/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "secret123"
  }'
```

**Autenticação de Cliente API:**
```bash
curl -X POST http://localhost:5051/api/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
```

**Refresh de Token de Cliente:**
```bash
curl -X POST http://localhost:5051/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "seu-refresh-token-aqui"
  }'
```

**Chamar endpoint protegido (usando qualquer tipo de token):**
```bash
curl http://localhost:5051/api/users \
  -H "Authorization: Bearer <token>"
```

**Criar usuário autenticado:**
```bash
curl -X POST http://localhost:5051/api/users \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Another User",
    "email": "another@example.com",
    "password": "secret123"
  }'
```

---

## 🗄️ **Modelo de Dados**

### User (Usuário)
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### ApiClient (Cliente de API)
```csharp
public class ApiClient
{
    public Guid Id { get; set; }
    public string ClientId { get; set; }
    public string ApiKeyHash { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
```

### ClientRefreshToken (Refresh Token de Cliente)
```csharp
public class ClientRefreshToken
{
    public Guid Id { get; set; }
    public Guid ApiClientId { get; set; }
    public string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
}
```

---

## 🐳 **Infraestrutura (Docker Compose)**

Serviços disponíveis:
- **PostgreSQL**: `localhost:5432` (appdb)
- **MongoDB**: `localhost:27017` (CrudIoLogs)
- **Redis**: `localhost:6379` (cache, RedisInsight em :5540)
- **Nginx**: `localhost:80/443` (reverse proxy)

**Variáveis de Ambiente** (`.env`):
```
# Cliente API
CLIENT_ID=crudio-client
CLIENT_API_KEY="&,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
CLIENT_NAME=CrudIo Integration

# Postgres
POSTGRES_DB=appdb
POSTGRES_USER=appuser
POSTGRES_PASSWORD=appuser

# MongoDB
MONGO_ROOT_USERNAME=appuser
MONGO_ROOT_PASSWORD=appuser

# Redis
REDIS_PASSWORD=appuser

# JWT (valores padrão usados se não definidos)
# JWT_SECRET=change-this-secret-in-production
# JWT_ISSUER=CrudIo.Api
# JWT_AUDIENCE=CrudIo.Client
# JWT_EXPIRATION_MINUTES=60
# CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
# CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS=30
```
> **Nota:** As variáveis de JWT têm valores padrão no código, mas podem ser sobrescritas definindo-as no `.env` ou no ambiente.

---

## 📚 **Endpoints da API**

Base URL local:
```text
http://localhost:5051
```

### Auth

| Método | Rota | Proteção | Descrição |
|--------|------|----------|-----------|
| `POST` | `/api/auth/login` | Pública | Autentica credenciais de usuário e retorna JWT |
| `POST` | `/api/auth/client-token` | Pública | Autentica credenciais de cliente API e retorna par de tokens (access + refresh) |
| `POST` | `/api/auth/refresh-token` | Pública | Renova o access token usando um refresh token válido (apenas para clientes API) |

### Users (Exigem autenticação JWT)

| Método | Rota | Proteção | Descrição |
|--------|------|----------|-----------|
| `POST` | `/api/users` | JWT | Cria usuário |
| `GET` | `/api/users/{id}` | JWT | Busca usuário por ID |
| `GET` | `/api/users?page=1&pageSize=10` | JWT | Lista usuários com paginação |
| `PUT` | `/api/users/{id}` | JWT | Atualiza usuário |
| `DELETE` | `/api/users/{id}` | JWT | Exclui usuário |

---

## 📖 **Documentação dos Endpoints**

### `POST /api/auth/login`
Autentica um usuário existente e retorna um JWT de acesso.

**Request:**
```json
{
  "email": "test@example.com",
  "password": "secret123"
}
```

**Response `200 OK`:**
```json
{
  "token": "jwt-token",
  "expiresIn": 216000,
  "expiresAt": "2026-06-22T18:00:00Z"
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Payload inválido |
| `401` | E-mail ou senha inválidos |
| `500` | Erro inesperado |

---

### `POST /api/auth/client-token`
Autentica um cliente API usando credenciais de serviço e retorna um par de tokens (access token e refresh token).

**Request Headers:**
```http
client-id: crudio-client
client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2
```

**Response `200 OK`:**
```json
{
  "accessToken": "jwt-access-token",
  "refreshToken": "jwt-refresh-token",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "expiresAt": "2026-06-19T18:15:00Z",
  "refreshExpiresAt": "2026-07-19T18:00:00Z"
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Credenciais de cliente inválidas ou inativas |
| `500` | Erro inesperado |

---

### `POST /api/auth/refresh-token`
Renova o access token de um cliente API usando um refresh token válido.

**Request:**
```json
{
  "refreshToken": "seu-refresh-token-aqui"
}
```

**Response `200 OK`:**
```json
{
  "accessToken": "novo-jwt-access-token",
  "refreshToken": "novo-jwt-refresh-token",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "expiresAt": "2026-06-19T18:15:00Z",
  "refreshExpiresAt": "2026-07-19T18:00:00Z"
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Refresh token inválido, expirado ou revogado |
| `400` | Payload inválido |
| `500` | Erro inesperado |

---

### `POST /api/users`
Cria um novo usuário. Exige autenticação JWT (qualquer token válido: de usuário ou de cliente API).

**Headers:**
```http
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "name": "Another User",
  "email": "another@example.com",
  "password": "secret123"
}
```

**Response `201 Created`:**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Another User",
  "email": "another@example.com",
  "createdAt": "2026-06-19T17:00:00Z"
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Payload inválido |
| `401` | Token ausente, inválido ou expirado |
| `500` | Erro inesperado |

---

### `GET /api/users`
Lista usuários com paginação. Exige autenticação JWT.

**Headers:**
```http
Authorization: Bearer <token>
```

**Query params:**
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `page` | `int` | `1` | Página atual |
| `pageSize` | `int` | `10` | Quantidade de itens por página |

**Exemplo:**
```http
GET /api/users?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Test User",
      "email": "test@example.com",
      "createdAt": "2026-06-19T17:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Token ausente, inválido ou expirado |
| `500` | Erro inesperado |

---

### `GET /api/users/{id}`
Busca um usuário específico por ID. Exige autenticação JWT.

**Headers:**
```http
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Test User",
  "email": "test@example.com",
  "createdAt": "2026-06-19T17:00:00Z",
  "updatedAt": null
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Token ausente, inválido ou expirado |
| `404` | Usuário não encontrado |
| `500` | Erro inesperado |

---

### `PUT /api/users/{id}`
Atualiza o nome e/ou e-mail de um usuário existente. Exige autenticação JWT.

**Headers:**
```http
Authorization: Bearer <token>
Content-Type: application/json
```

**Request:**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Updated User",
  "email": "updated@example.com"
}
```

**Response `200 OK`:**
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Updated User",
  "email": "updated@example.com",
  "createdAt": "2026-06-19T17:00:00Z",
  "updatedAt": "2026-06-19T17:30:00Z"
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Payload inválido ou ID da rota diferente do ID do body |
| `401` | Token ausente, inválido ou expirado |
| `404` | Usuário não encontrado |
| `409` | E-mail já cadastrado por outro usuário |
| `500` | Erro inesperado |

---

### `DELETE /api/users/{id}`
Exclui um usuário existente. Exige autenticação JWT.

**Headers:**
```http
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{
  "success": true,
  "message": "User deleted successfully."
}
```

**Possíveis erros:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Token ausente, inválido ou expirado |
| `404` | Usuário não encontrado |
| `500` | Erro inesperado |

---

## 📝 **Formato de Erro**

Erros tratados pela API retornam o formato padronizado:
```json
{
  "code": "ERROR_CODE",
  "message": "Descrição legível do erro",
  "statusCode": 400,
  "traceId": "identificador-único-de-rastreamento"
}
```

Exemplo:
```json
{
  "code": "USER_NOT_FOUND",
  "message": "User not found.",
  "statusCode": 404,
  "traceId": "0HN..."
}
```

---

## ✅ **Pontos Fortes**

- ✨ **Arquitetura moderna**: Vertical Slice + CQRS + Minimal APIs
- 🔒 **Segurança robusta**: JWT com dois fluxos (usuário e cliente), senhas hashgeadas, validação rigorosa
- 📊 **Observabilidade**: Logging estruturado em MongoDB + health checks
- ⚡ **Performance**: Cache Redis, paginação eficiente
- 🐳 **DevOps pronto**: Totalmente containerizado com docker-compose
- 📓 **Documentação**: OpenAPI/Swagger integrado
- 🎯 **Manutenibilidade**: Baixo acoplamento entre features, padrões consistentes
- 🔄 **Refresh Tokens**: Suporte a renovação segura de tokens para clientes API

---

## 🔄 **Ciclo de Desenvolvimento**

1. **Criar nova feature** → nova pasta em `Features/`
2. **Definir Command/Query** → arquivo `.cs` com DTOs
3. **Criar Handler** → implementar lógica com EF Core
4. **Adicionar Validador** → FluentValidation (pipeline automático)
5. **Mapear endpoints** → `[Feature]Endpoints.cs`
6. **Testar** → rotas na API (use a coleção Insomnia atualizada para testes rápidos)

---

## 📝 **Licença**

Este projeto está sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

---
*Atualizado em: 19 de junho de 2026*# crudio
# crudio
