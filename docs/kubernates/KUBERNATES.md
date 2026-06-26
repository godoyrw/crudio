Lembre-se das diretrizes do seu CLAUDE.md:

❓ Não assumir – Vamos verificar cada passo antes de continuar
📋 Solicitar evidências/logs – Vamos olhar os outputs juntos
🔧 Primeiro entender, depois aplicar – Explico antes de você executar

📌 Vamos começar pelo Passo 0: Verificação do ambiente

✅ O que vamos verificar:
Se você tem as ferramentas necessárias instaladas (kind, kubectl, docker)
Se o Docker está rodando (necessário para o kind funcionar)
Se estamos no diretório correto do projeto

▶️ Você executa agora:
# Confirmar que estamos no diretório do projeto
```bash
pwd
```
⚠️ Importante: Você deve ver algo como:
Saida:

```bash
/mnt/02 - OWD/.owd/.oct/.pus/.wd/.files/03 - Projetos/02 - Clients/01 - NFE.io/01 CrudIo
```

```bash
# Verificar versões das ferramentas
kind version
kubectl version --client
docker version --format '{{.Server.Version}}'
```


```bash
godoy@ODS-LNX01:/mnt/02 - OWD/.owd/.oct/.pus/.wd/.files/03 - Projetos/02 - Clients/01 - NFE.io/01 CrudIo$ 
# Verificar versões das ferramentas
kind version
kubectl version --client
docker version --format '{{.Server.Version}}'

# Confirmar que estamos no diretório do projeto
pwd
kind v0.23.0 go1.21.10 linux/amd64
Client Version: v1.35.5
Kustomize Version: v5.7.1
29.5.3
/mnt/02 - OWD/.owd/.oct/.pus/.wd/.files/03 - Projetos/02 - Clients/01 - NFE.io/01 CrudIo
```


🔍 Minha explicação (enquanto você executa):
Comando	O que faz	Por que é relevante para o projeto
kind version	Mostra a versão do Kubernetes IN Docker que vamos usar	O kind nos permite rodar um cluster Kubernetes dentro de containers Docker – perfeito para estudo pois você já usa Docker Compose. Nada de VMs pesadas ou configuração de hypervisor.
kubectl version --client	Mostra a versão do cliente Kubernetes	O kubectl é a "controle remoto" padrão para falar com qualquer cluster Kubernetes (local, EKS, GKE, etc.). Vamos usá-lo para aplicar nossos manifests.
docker version	Confirme que o servidor está rodando`	Verifica se o Docker daemon está ativo
pwd	Mostra o diretório atual	Garantimos que estamos na raiz do projeto onde o Dockerfile, k8s/ e src/ existem. Todos os caminhos relativos nos manifests dependem disso.

✅ O que você deve ver como saída (exemplo):

kind v0.23.0 go1.21.10 linux/amd64
Client Version: version.Info{Major:"1", Minor:"29", GitVersion:"v1.29.0", ...}
Server Version: 26.1.4
/mnt/02 - OWD/.owd/.oct/.pus/.wd/.files/03 - Projetos/02 - Clients/01 - NFE.io/01 CrudIo
Se tudo estiver correto, partimos para o Passo 1: Criando o cluster Kind.


Passo 1: Criando o cluster Kind
▶️ Você executa agora:

# Criar um cluster Kind chamado "crudio-local" com mapeamento de portas
kind create cluster --name crudio-local --config - <<EOF
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4

nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 30001
    hostPort: 30001
    protocol: TCP
  - containerPort: 30002
    hostPort: 30002
    protocol: TCP

networking:
  disableDefaultCNI: false
EOF

# Aguardar o cluster ficar pronto (pode levar 10-30 segundos)
kubectl wait --for=condition=ready node --timeout=90s --all

# Verificar os nós do cluster
kubectl get nodes

godoy@ODS-LNX01:/mnt/02 - OWD/.owd/.oct/.pus/.wd/.files/03 - Projetos/02 - Clients/01 - NFE.io/01 CrudIo$ 
# Deletar cluster antigo (se existir) e criar novo com sua configuração
kind delete cluster --name crudio-local 2>/dev/null || echo "Cluster não existia, criando novo..."

kind create cluster --name crudio-local --config - <<EOF
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  extraPortMappings:
  # API .NET (CrudIo.Api) - será acessível em localhost:30001
  - containerPort: 30001
    hostPort: 30001
    protocol: TCP
  
  # Nginx (entrada principal opcional) - será acessível em localhost:8080
  - containerPort: 30080
    hostPort: 8080
    protocol: TCP

  # RedisInsight UI - será acessível em localhost:5540 (correspondendo ao docker-compose)
  - containerPort: 5540
    hostPort: 5540
    protocol: TCP

  # Opcional: debug / metrics / outros serviços
  - containerPort: 30003
    hostPort: 30003
    protocol: TCP
EOF

# Aguardar o cluster ficar pronto (pode levar 10-30 segundos)
kubectl wait --for=condition=ready node --timeout=90s --all

# Verificar os nós do cluster
kubectl get nodes
Creating cluster "crudio-local" ...
 ✓ Ensuring node image (kindest/node:v1.30.0) 🖼 
 ✓ Preparing nodes 📦  
 ✓ Writing configuration 📜 
 ✓ Starting control-plane 🕹️ 
 ✓ Installing CNI 🔌 
 ✓ Installing StorageClass 💾 
Set kubectl context to "kind-crudio-local"
You can now use your cluster with:

kubectl cluster-info --context kind-crudio-local

Thanks for using kind! 😊
node/crudio-local-control-plane condition met
NAME                         STATUS   ROLES           AGE   VERSION
crudio-local-control-plane   Ready    control-plane   20s   v1.30.0


🔍 Minha explicação (enquanto você executa):
📦 O que é um "cluster Kind"?
É um cluster Kubernetes mínimo (geralmente 1 nó) onde cada nó é um container Docker rodando em sua máquina.
É ideal para desenvolvimento e testes locais porque:
Rápido de criar/destruir (segundos)
Não consome muitos recursos (reutiliza seu Docker existente)
Espelha fielmente o comportamento de um cluster "real" (mesmo API Kubernetes)
🔌 Por que mapeamos as portas 30001 e 30002?
No seu projeto original com Docker Compose, você acessava:
API: http://localhost:5051
RedisInsight: http://localhost:5540
No Kubernetes, vamos expor esses serviços via NodePort (portas altas no nó, como 30000-32767).
Mapeamos containerPort: 30001 → hostPort: 30001 para que, quando um serviço no cluster usar a porta 30001, você possa acessá-lo em localhost:30001 na sua máquina host.
Isso substitui o papel do ports: no seu docker-compose.yaml, mas feito no nível do nó Kubernetes.
🧱 O que é o "control-plane"?
É o nó que gerencia o cluster (agenda pods, mantém estado, expõe a API do Kubernetes).
No kind de nó único, o mesmo nó que roda suas aplicações também roda o control-plane (não é ideal para produção, mas perfeito para estudo).
📊 O que o kubectl wait faz?
Aguarda até que todos os nós estejam em estado Ready antes de continuar.
Evita que você aplique manifests enquanto o cluster ainda está inicializando (o que causaria falhas).
✅ O que você deve ver como saída:

✓ Ensuring node image (kindest/node:v1.29.0) 🖼
✓ Preparing nodes 📦
✓ Writing configuration 📜
✓ Starting control-plane 🕹️
✓ Installing CNI 🔌
✓ Installing StorageClass 💾
✓ Joined worker nodes �clusterização
✓ Setting up rootfs 🖼
✓ Ready! 🎉
node/"crudio-local-control-plane" condition met
NAME                             STATUS   ROLES           AGE   VERSION
crudio-local-control-plane       Ready    control-plane   2m    v1.29.0
Se você ver STATUS: Ready e ROLES: control-plane, o cluster está pronto!

❓ Pergunta de validação (nosso momento "Não assumir!"):
Por que não usamos o Minikube em vez do Kind para este estudo local?

Pense um momento antes de responder – isso ajuda a fixar o conceito.

Minha perspectiva (depois que você pensar):

Ambos são válidos, mas o Kind é mais simples para seu caso porque:
Não precisa de hypervisor (VirtualBox/Hyper-V) – roda diretamente em containers Docker (que você já tem rodando para o docker-compose).
Inicialização mais rápida (~10s vs ~1-2min do Minikube).
Integração direta com seu fluxo de trabalho Docker – as mesmas imagens que você builda para o docker-compose podem ser carregadas no Kind com kind load docker-image.
Menos camadas de abstração – mais fácil de entender o que está acontecendo por baixo.
Se o cluster estiver Ready, partimos para o Passo 2: Buildar e carregar as imagens Docker.


▶️ Você executa agora (Passo 2):

# Construir a imagem da aplicação CrudIo.Api
# (Do diretório raiz do projeto onde o Dockerfile está)
```bash
# 1. Reconstruir a imagem da aplicação CrudIo.Api com o Dockerfile corrigido
docker build -t crudio-api:local .

# 2. Carregar esta imagem para dentro do cluster Kind
kind load docker-image crudio-api:local --name crudio-local

# 3. Carregar as imagens oficiais dos serviços de infraestrutura
echo "Carregando postgres:18..."
kind load docker-image postgres:18 --name crudio-local

echo "Carregando mongo:8..."
kind load docker-image mongo:8 --name crudio-local

echo "Carregando redis:8-alpine..."
kind load docker-image redis:8-alpine --name crudio-local

echo "Carregando redis/redisinsight:latest..."
kind load docker-image redis/redisinsight:latest --name crudio-local

# 4. Verificar quais imagens estão disponíveis nos nós (validar)
kubectl get nodes -o jsonpath='{.items[0].status.images[*]}' | tr ' ' '\n' | sort
```

Vamos fazer um de cada vez para você ver o output e eu explicar.


🔍 Explicação enquanto você executa o docker build:

Parte do Dockerfile	O que faz	Relação com o projeto
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build	Usa a imagem oficial do .NET SDK 10.0 como base para compilar	No seu projeto, você já usa .NET 10.0 (veja o src.csproj). Isto garante consistência entre seu ambiente local e o container.

COPY ["src/src.csproj", "./"] + dotnet restore	Copia apenas o csproj primeiro e restaura dependências	Aproveita o cache do Docker: se o csproj não mudar, não precisa restaurar novamente nas próximas builds.

COPY . . + dotnet build	Copia todo o código e compila	Equivalente ao dotnet build que você faria localmente, mas dentro do container.

FROM build AS publish + dotnet publish	Publica o app compilado em uma pasta otimizada	Remove arquivos de desenvolvimento (pdb, etc.) deixando só é prec está executando.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base	Usa a imagem de runtime ASP.NET 10.0 (menor que o SDK)	ImagemFinal apenas o necessário para rodar (~180MB vs ~1GB do SDK). Isso segue a boa prática de multi-stage build para imagens menores e mais seguras.

EXPOSE 5051 + ENV ASPNETCORE_URLS=http://+:5051	Expõe a porta 5051 e configura a ASP.NET para escutar em todas as interfaces	No seu Program.cs, a aplicação já escuta na porta 5051 (pelo docker-compose.yaml). Isto 
garante o mesmo comportamento.

COPY --from=publish /app/publish .	Copia o app published do estágio anterior	Só deixa o essencial na imagem final.

ENTRYPOINT ["dotnet", "CrudIo.Api.dll"]	Define o comando de entrada	É o mesmo que você rodaria com dotnet run ou dotnet CrudIo.Api.dll localmente.
✅ O que você deve ver após o docker build:

```bash
=> [internal] load build definition from Dockerfile
=> => transferring dockerfile: 32B
=> => ...
=> [internal] load metadata for mcr.microsoft.com/dotnet/sdk:10.0
=> => ...
=> [internal] load .dockerignore
=> => transferring .dockerignore: 34B
=> ...
=> [build Stage 1/3] FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:...
=> ...
=> => restoring packages in 1.2s
=> ...
=> => restoring packages: 100%, 8.72k restored
=> => restoring packages: 224ms
=> => => resolving packages: 224ms
=> ...
=> => restoring packages: 151ms
=> => =>
```
