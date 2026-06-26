# 📊 Resumo do Projeto **CrudIo.Api**

## 🎯 **O que é?**
Uma **API RESTful em .NET 10** para gerenciar usuários, seguindo **Vertical Slice Architecture**. Projeto cliente para a plataforma Crud.io com integração completa a infraestrutura em containers (PostgreSQL, MongoDB, Redis) orquestrada com Kubernetes.

### Resumo da Análise do Projeto
O CrudIo.Api é uma API RESTful em .NET 10 com:

Arquitetura: Vertical Slice + CQRS + Minimal APIs
Banco de Dados: PostgreSQL 18 (dados principais), MongoDB 8 (logs)
Cache: Redis 8
Proxy: NGINX Ingress Controller (via Kubernetes Services)
Autenticação: JWT com dois fluxos (usuário e cliente API)
Orquestração Atual: Kubernetes (Kind local)

---

## 🏗️ **Arquitetura & Stack**

| Camada | Tecnologia |
|--------|-----------|
| **Framework** | ASP.NET Core 10 (Minimal APIs) |
| **Banco Relacional** | PostgreSQL 18 |
| **Banco NoSQL** | MongoDB 8 (logs) |
| **Cache** | Redis 8 |
| **Service Mesh / Networking** | Kubernetes Services (ClusterIP, NodePort) |
| **Proxy/Load Balancer** | NGINX Ingress Controller (via Services) |
| **CQRS & Validação** | MediatR + FluentValidation |
| **ORM** | Entity Framework Core 10 |
| **Documentação** | OpenAPI (Swagger) |
| **Orquestração** | Kubernetes (StatefulSets, Deployments, Services) |
| **Armazenamento Persistente** | hostPath (dev/local), recomendado: cloud disks em produção |
| **Gerenciamento de Configuração** | ConfigMaps e Secrets |

---

## 📁 **Estrutura do Projeto**

```
k8s/
├── api-deployment.yaml                               (Deployment da API + Service NodePort :30001)
├── configmaps.yaml                                   (Variáveis não-sensíveis — hosts, portas, nomes de banco)
├── mongodb-statefulset.yaml                          (StatefulSet MongoDB 8 + Service ClusterIP :27017)
├── namespace.yaml                                    (Namespace crudio-local)
├── postgres-statefulset.yaml                         (StatefulSet PostgreSQL 18 + Service ClusterIP :5432)
├── redis-deployment.yaml                             (Deployment Redis 8 + Service ClusterIP :6379)
├── redisinsight-deployment.yaml                      (Deployment RedisInsight + Service NodePort :30002)
└── secrets.yaml                                      (Dados sensíveis — senhas, JWT secret, API key)

src/                
├── CrudIo.Api/               
│   ├── Features/                                     (Funcionalidades organizadas por domínio - Vertical Slice)
│   │   ├── Auth/                                     (Autenticação e autorização)
│   │   │   ├── Login/                                (POST /api/v1/auth/login — autenticação de usuário com email/senha)
│   │   │   ├── RefreshToken/                         (POST /api/v1/auth/refresh-token — renovação de token de cliente)
│   │   │   └── ClientToken/                          (POST /api/v1/auth/client-token — autenticação service-to-service)
│   │   └── Users/                                    (Gerenciamento de usuários)
│   │       ├── CreateUser/                           (POST /api/v1/users — cria novo usuário autenticado)
│   │       ├── GetUser/                              (GET /api/v1/users/{id} — busca usuário por ID)
│   │       ├── UpdateUser/                           (PUT /api/v1/users/{id} — atualiza dados do usuário)
│   │       ├── DeleteUser/                           (DELETE /api/v1/users/{id} — remove usuário)
│   │       ├── ListUsers/                            (GET /api/v1/users?page=1&pageSize=10 — lista paginada)
│   │       └── UsersEndpoints.cs                     (Mapper de rotas — registra os endpoints no pipeline do ASP.NET)
│   │               
│   ├── Data/                                         (Camada de acesso a dados)
│   │   ├── AppDbContext.cs                           (EF Core DbContext — contexto principal do PostgreSQL)
│   │   ├── Entities/                                 (Entidades mapeadas pelo EF Core)
│   │   │   ├── User.cs                               (Entidade de usuário)
│   │   │   ├── ApiClient.cs                          (Entidade de cliente API para autenticação service-to-service)
│   │   │   └── ClientRefreshToken.cs                 (Entidade de refresh token de cliente)
│   │   └── EntityConfigurations/                     (Configurações Fluent API do EF Core)
│   │       ├── UserConfiguration.cs                  (Mapeamento da entidade User)
│   │       ├── ApiClientConfiguration.cs             (Mapeamento da entidade ApiClient)
│   │       └── ClientRefreshTokenConfiguration.cs    (Mapeamento da entidade ClientRefreshToken)
│   │               
│   ├── Common/                                       (Infraestrutura transversal compartilhada)
│   │   ├── Behaviors/                                (Pipelines do MediatR executados antes/depois dos handlers)
│   │   │   └── ValidationBehavior.cs                 (Executa validações FluentValidation automaticamente)
│   │   ├── Middleware/                               (Middlewares customizados do pipeline HTTP)
│   │   ├── Security/                                 (Serviços de segurança e autenticação)
│   │   │   ├── JwtService.cs                         (Geração e validação de tokens JWT)
│   │   │   ├── PasswordService.cs                    (Hash e verificação de senhas com BCrypt)
│   │   │   ├── RefreshTokenService.cs                (Geração e rotação de refresh tokens)
│   │   │   └── ApiClientSeeder.cs                    (Seed inicial do cliente API no startup)
│   │   ├── Validation/                               (Helpers e extensões de validação)
│   │   ├── Pagination/                               (Modelos e helpers de paginação)
│   │   └── HealthChecks/                             (Verificações de saúde dos serviços)
│   │               
│   ├── Cache/                                        (Camada de cache com Redis)
│   │   ├── RedisCacheService.cs                      (Implementação de get/set/invalidate no Redis)
│   │   └── CacheKeys.cs                              (Constantes e builders de chaves de cache)
│   │               
│   ├── Logging/                                      (Logging estruturado no MongoDB)
│   │   ├── MongoLogger.cs                            (Gravação de logs no MongoDB)
│   │   └── LogEntry.cs                               (Modelo de documento de log)
│   │               
│   ├── Migrations/                                   (EF Core Migrations — versionamento do schema do PostgreSQL)
│   ├── Program.cs                                    (Entry point — DI, middlewares, endpoints e startup)
│   └── appsettings.json                              (Configurações da aplicação por ambiente)
│               
└── Properties/                                       (Configurações de launch e perfis de execução)
```               

---               

## 🌐 **Deploy com Kubernetes**

O projeto está configurado para deploy em um cluster Kubernetes local usando Kind (Kubernetes IN Docker). A estrutura de manifests está no diretório `k8s/`:

```
k8s/
├── namespace.yaml                 # Namespace crudio-local
├── secrets.yaml                   # Dados sensíveis (senhas, chaves)
├── configmaps.yaml                # Configurações não-sensíveis
├── postgres-statefulset.yaml      # PostgreSQL 18 com storage persistente
├─ mongodb-statefulset.yaml        # MongoDB 8 com storage persistente
├── redis-deployment.yaml          # Redis 8 cache
├── api-deployment.yaml            # CrudIo.Api ASP.NET Core
├── redisinsight-deployment.yaml   # Interface gráfica para Redis (opcional)
└── kustomization.yaml             # (opcional) para gestão de ambiente
```

### Características do Deploy Kubernetes:
- **StatefulSets** para PostgreSQL e MongoDB (garantia de identidade estável e storage persistente)
- **Deployments** para aplicações stateless (API, Redis, RedisInsight)
- **Services** internos (ClusterIP) e externos (NodePort) para acesso
- **ConfigMaps e Secrets** para gestão de configuração e dados sensíveis
- **Probes de saúde** (liveness/readiness) configurados para cada componente
- **Requests e Limits** de recursos definidos para qualidade de serviço
- **Volumes**: hostPath para desenvolvimento local (substituir por cloud storage em produção)

### Acessando os Serviços:
Após aplicar os manifests com `kubectl apply -f k8s/`:
- **API CrudIo**: `http://localhost:30001` (NodePort Service)
- **RedisInsight**: `http://localhost:30002` (NodePort Service, se habilitado)
- **Bancos de dados**: Acessíveis apenas dentro do cluster via seus serviços Kubernetes
  - PostgreSQL: `postgres.crudio-local.svc.cluster.local:5432`
  - MongoDB: `mongodb.crudio-local.svc.cluster.local:27017`
  - Redis: `redis.crudio-local.svc.cluster.local:6379`

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
1. Autentique em `POST /api/v1/auth/login` usando `email` e `password`.
2. Guarde o campo `token` retornado.
3. Envie o token nas rotas protegidas usando o header:
   ```http
   Authorization: Bearer <token>
   ```

### **Fluxo de Cliente API (Service-to-Service)**
1. Autentique em `POST /api/v1/auth/client-token` usando os headers:
   - `client-id`: Seu ID de cliente
   - `client-api-key`: Sua chave de API
2. Guarde os campos `accessToken` e `refreshToken` retornados.
3. Use o `accessToken` nas rotas protegidas da mesma forma que o token de usuário.
4. Quando o accessToken expirar, use o `refreshToken` em `POST /api/v1/auth/refresh-token` para obter um novo par de tokens.

> **Observação:** Como não existe cadastro público de usuários, o primeiro usuário deve ser provisionado de forma administrativa (seed, migration/manual no banco ou outro processo interno). Depois disso, usuários autenticados podem criar novos usuários por `POST /api/v1/users`.

### Configuração JWT

As configurações são lidas por variáveis de ambiente, com valores configurados atualmente no arquivo `.env`:

| Variável | Descrição | Valor Atual |
|----------|-----------|-------------|
| `JWT_SECRET` | Chave usada para assinar o token | Configurada no `.env` (valor seguro não divulgado) |
| `JWT_ISSUER` | Emissor esperado do token | `CrudIo.Api` |
| `JWT_AUDIENCE` | Audiência esperada do token | `CrudIo.Client` |
| `JWT_EXPIRATION_MINUTES` | Tempo de expiração do access token de usuário em minutos | `60` (1 hora) |
| `CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES` | Tempo de expiração do access token de cliente em minutos | `15` |
| `CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS` | Tempo de expiração do refresh token de cliente em dias | `30` |

Em produção, configure obrigatoriamente um `JWT_SECRET` forte e privado.

### Exemplo de Uso com cURL

**Login de Usuário:**
```bash
curl -X POST http://localhost:30001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "secret123"
  }'
```

**Autenticação de Cliente API:**
```bash
curl -X POST http://localhost:30001/api/v1/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
```

**Refresh de Token de Cliente:**
```bash
curl -X POST http://localhost:30001/api/v1/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "seu-refresh-token-aqui"
  }'
```

**Chamar endpoint protegido (usando qualquer tipo de token):**
```bash
curl http://localhost:30001/api/v1/users \
  -H "Authorization: Bearer <token>"
```

**Criar usuário autenticado:**
```bash
curl -X POST http://localhost:30001/api/v1/users \
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
- 🐳 **DevOps pronto**: Totalmente containerizado com Kubernetes
- 📓 **Documentação**: OpenAPI/Swagger integrado
- 🎯 **Manutenibilidade**: Baixo acoplamento entre features, padrões consistentes
- 🔄 **Refresh Tokens**: Suporte a renovação segura de tokens para clientes API

---

## 🩺 **Health Endpoint**

A API fornece um endpoint de health check para monitoramento:
- **GET** `/health` - Retorna `200 OK` quando a API está saudável
- Não requer autenticação
- Útil para Kubernetes liveness/readiness probes e load balancers

---

## 🔄 **Ciclo de Desenvolvimento com Kubernetes**

1. **Desenvolver e testar localmente** → `dotnet run` ou Docker
2. **Construir imagem Docker** → `docker build -t crudio-api:local .`
3. **Carregar imagem no Kind** → `kind load docker-image crudio-api:local --name crudio-local`
4. **Aplicar/atualizar manifests** → `kubectl apply -f k8s/`
5. **Verificar deploy** → `kubectl get pods -n crudio-local`
6. **Testar API** → `curl http://localhost:30001/api/v1/users`
7. **Iterar** → Repetir passos 2-6 conforme necessário

### Script de Deploy Local (disponível no projeto):
```bash
#!/bin/bash
set -e
echo "🔨 Building..."
docker build -t crudio-api:local .

echo "📦 Loading into kind..."
kind load docker-image crudio-api:local --name crudio-local

echo "🔄 Restarting..."
kubectl rollout restart deployment/crudio-api -n crudio-local
kubectl rollout status deployment/crudio-api -n crudio-local

echo "✅ Done!"
```

Execute com: `./deploy-local.sh`

---

## 📝 **Licença**

Este projeto está sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

---

*Atualizado em: 25 de junho de 2026*