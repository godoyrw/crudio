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
src/
├── CrudIo.Api/
│   ├── Features/
│   │   ├── Auth/
│   │   │   ├── Login/                 (POST /auth/login)
│   │   │   ├── RefreshToken/          (POST /auth/refresh-token)
│   │   │   └── ClientToken/           (POST /auth/client-token)
│   │   └── Users/
│   │       ├── CreateUser/            (POST /users)
│   │       ├── GetUser/               (GET /users/{id})
│   │       ├── UpdateUser/            (PUT /users/{id})
│   │       ├── DeleteUser/            (DELETE /users/{id})
│   │       ├── ListUsers/             (GET /users?page=1&pageSize=10)
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

## 🌐 **Deploy com Kubernetes**

O projeto está configurado para deploy em um cluster Kubernetes local usando Kind (Kubernetes IN Docker). A estrutura de manifests está no diretório `k8s/`:

```
k8s/
├── namespace.yaml                 # Namespace crudio-local
├── secrets.yaml                   # Dados sensíveis (senhas, chaves)
├── configmaps.yaml                # Configurações não-sensíveis
├── postgres-statefulset.yaml      # PostgreSQL 18 com storage persistente
├─ mongodb-statefulset.yaml       # MongoDB 8 com storage persistente
├── redis-deployment.yaml          # Redis 8 cache
├── api-deployment.yaml           # CrudIo.Api ASP.NET Core
├── redisinsight-deployment.yaml  # Interface gráfica para Redis (opcional)
└── kustomization.yaml            # (opcional) para gestão de ambiente
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
(Manter o conteúdo existente desta seção, pois é relevante independentemente do deploy)

---

## 🔐 **Autenticação JWT**
(Manter o conteúdo existente desta seção, pois é relevante independentemente do deploy)

---

## 🗄️ **Modelo de Dados**
(Manter o conteúdo existente desta seção, pois é relevante independentemente do deploy)

---

## 🐳 **Variáveis de Ambiente (.env)**
As variáveis de ambiente são usadas tanto para o desenvolvimento local quanto para configurar o cluster Kubernetes via Secrets e ConfigMaps.

```
# Cliente API
CLIENT_ID=crudio-client
CLIENT_API_KEY="&,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
CLIENT_NAME=CrudIo Integration

# Postgres
POSTGRES_HOST=postgres          # Nome do servizio Kubernetes internamente
POSTGRES_PORT=5432
POSTGRES_DB=appdb
POSTGRES_USER=appuser
POSTGRES_PASSWORD=appuser

# MongoDB
MONGO_ROOT_USERNAME=appuser
MONGO_ROOT_PASSWORD=appuser

# Redis
REDIS_PASSWORD=appuser

# JWT
JWT_SECRET="6Y59~2+(]G4!r/k_;1*<p;1u^Q>U=wXn5m3V}F1k9?-D&,C+^$\j_9c7R9r[/Q^XC6U^:hDJ"
JWT_ISSUER=CrudIo.Api
JWT_AUDIENCE=CrudIo.Client
JWT_EXPIRATION_MINUTES=3600
CLIENT_ACCESS_TOKEN_EXPIRATION_MINUTES=15
CLIENT_REFRESH_TOKEN_EXPIRATION_DAYS=30
```

> **Nota importantes para Kubernetes:**
> 1. Quando rodando dentro do cluster, os hosts dos serviços são os nomes dos Kubernetes Services (ex: `postgres`, `mongodb`, `redis`)
> 2. Para desenvolvimento local com porta forwarding, use `localhost` com as NodePorts configuradas
> 3. As variáveis acima são usadas para gerar o `secrets.yaml` e `configmaps.yaml` aplicados ao cluster
> 4. Em produção, recomenda-se usar um cofre de secrets externos (AWS Secrets Manager, HashiCorp Vault, etc.)

---

## 📚 **Endpoints da API**

Base URL no Kubernetes Kind (NodePort):
```text
http://localhost:30001
```

### Auth

| Método | Rota | Proteção | Descrição |
|--------|------|----------|-----------|
| `POST` | `/auth/login` | Pública | Autentica credenciais de usuário e retorna JWT |
| `POST` | `/auth/client-token` | Pública | Autentica credenciais de cliente API e retorna par de tokens (access + refresh) |
| `POST` | `/auth/refresh-token` | Pública | Renova o access token usando um refresh token válido (apenas para clientes API) |

### Users (Exigem autenticação JWT)

| Método | Rota | Proteção | Descrição |
|--------|------|----------|-----------|
| `POST` | `/users` | JWT | Cria usuário |
| `GET` | `/users/{id}` | JWT | Busca usuário por ID |
| `GET` | `/users?page=1&pageSize=10` | JWT | Lista usuários com paginação |
| `PUT` | `/users/{id}` | JWT | Atualiza usuario |
| `DELETE` | `/users/{id}` | JWT | Exclui usuario |

---

## 📖 **Documentação dos Endpoints**
(Manter o conteúdo existente das seções de documentação dos endpoints, mas atualizar os exemplos de cURL para usar localhost:30001)

### Exemplos de cURL atualizados para Kubernetes Kind:

**Login de Usuário:**
```bash
curl -X POST http://localhost:30001/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "secret123"
  }'
```

**Autenticação de Cliente API:**
```bash
curl -X POST http://localhost:30001/auth/client-token \
  -H "client-id: crudio-client" \
  -H "client-api-key: &,M:8<bi|5=NmnG&P?dJ=ibriyx|6bG|V/r+p-D&#c:p84N)=2"
```

**Refresh de Token de Cliente:**
```bash
curl -X POST http://localhost:30001/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "seu-refresh-token-aqui"
  }'
```

**Chamar endpoint protegido (usando qualquer tipo de token):**
```bash
curl http://localhost:30001/users \
  -H "Authorization: Bearer <token>"
```

**Criar usuário autenticado:**
```bash
curl -X POST http://localhost:30001/users \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Another User",
    "email": "another@example.com",
    "password": "secret123"
  }'
```

---

## 📝 **Formato de Erro**
(Manter o conteúdo existente desta seção)

---

## ✅ **Pontos Fortes**
(Manter o conteúdo existente desta seção)

---

## 🔄 **Ciclo de Desenvolvimento com Kubernetes**

1. **Desenvolver e testar localmente** → `dotnet run` ou Docker
2. **Construir imagem Docker** → `docker build -t crudio-api:local .`
3. **Carregar imagem no Kind** → `kind load docker-image crudio-api:local --name crudio-local`
4. **Aplicar/atualizar manifests** → `kubectl apply -f k8s/`
5. **Verificar deploy** → `kubectl get pods -n crudio-local`
6. **Testar API** → `curl http://localhost:30001/api/users`
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