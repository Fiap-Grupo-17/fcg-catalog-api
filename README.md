# FCG.CatalogAPI

Microsserviço de **Catálogo e Biblioteca** da plataforma Fiap Cloud Games.

## Responsabilidades

- CRUD de jogos (Admin)
- Listagem pública do catálogo
- Início de aquisição de jogo → publica `OrderPlacedEvent` (retorna 202 Accepted)
- Consome `PaymentProcessedEvent` → adiciona jogo à biblioteca ou marca pedido como rejeitado

## Fluxo de compra async

```
POST /api/biblioteca/aquisicoes
        │
        ▼
  Cria Pedido (status=Pendente)
  Publica OrderPlacedEvent
        │
        ▼
  [PaymentsAPI processa]
        │
        ▼
  PaymentProcessedConsumer (CatalogAPI)
    ├─ Approved → ItemBiblioteca criado, Pedido=Aprovado
    └─ Rejected → Pedido=Rejeitado
```

## Endpoints

| Método | Rota                           | Auth    | Descrição                      |
|--------|--------------------------------|---------|--------------------------------|
| GET    | /health                        | —       | Health check                   |
| GET    | /api/jogos                     | —       | Lista catálogo ativo           |
| POST   | /api/jogos                     | Admin   | Cria novo jogo                 |
| POST   | /api/biblioteca/aquisicoes     | JWT     | Inicia compra (202 Accepted)   |
| GET    | /api/biblioteca                | JWT     | Lista biblioteca do usuário    |

## Variáveis de Ambiente

| Variável                        | Descrição                          |
|---------------------------------|------------------------------------|
| `ConnectionStrings__Postgres`   | String de conexão PostgreSQL       |
| `Jwt__Secret`                   | Segredo JWT (igual ao UsersAPI)    |
| `Jwt__Issuer`                   | `fcg-users-api`                    |
| `Jwt__Audience`                 | `fcg-platform`                     |
| `RabbitMQ__Host`                | Host do RabbitMQ                   |
| `RabbitMQ__Password`            | Senha RabbitMQ *(via Secret)*      |
| `Redis__ConnectionString`       | String de conexão Redis (`host:porta`) |
| `Mongo__ConnectionString`       | String de conexão MongoDB *(via Secret)* |
| `Mongo__Database`               | `fcg_catalog`                      |


## CI/CD (GitHub Actions)

O workflow `.github/workflows/ci.yml` tem dois jobs:

- **build-and-test:** restaura, compila e roda os testes em todo push e pull request para a `main`. Os testes de integração usam um RabbitMQ como serviço do job.
- **deploy:** roda só em push na `main`. Faz login no Azure por OIDC (sem senha guardada), constrói a imagem, roda o scan de vulnerabilidades (Trivy), publica no ACR com a tag igual ao hash do commit e atualiza o Deployment no AKS com `kubectl set image` (Rolling Update, sem downtime). Se o rollout falhar, o pipeline desfaz com `rollout undo`.

**Configuração no GitHub** (Settings → Secrets and variables → Actions):

| Tipo | Nome | Observação |
|---|---|---|
| Secret | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | identificadores da identidade do pipeline, não são senhas |
| Variable | `ACR_NAME`, `ACR_LOGIN_SERVER`, `AKS_RESOURCE_GROUP`, `AKS_CLUSTER_NAME`, `K8S_NAMESPACE` | nomes dos recursos no Azure |

**Login por OIDC:** o GitHub prova ao Azure que o job vem deste repositório e da branch `main`, e recebe um acesso temporário. A organização usa o formato de subject com IDs (`repo:<org>@<id>/<repo>@<id>:ref:refs/heads/main`), então a credencial federada da identidade `id-fcg-github` foi cadastrada com esse texto exato.

**Regras do pipeline:** deploy só na `main`; sem `environment:` no job de deploy; tag da imagem = hash do commit (nunca `latest`); o pipeline não aplica manifestos, só troca a imagem (os manifestos ficam no `fcg-orchestration`).

## NoSQL (MongoDB) e Cache (Redis)

A base de persistência principal do catálogo é **PostgreSQL/EF Core** (jogos, pedidos,
biblioteca). MongoDB e Redis são usados como camadas complementares, específicas para
performance e para as necessidades de escrita/leitura de alto volume do catálogo:

### Caso de uso

- **Redis (cache-aside)** — evita bater no Postgres a cada listagem de catálogo, que é o
  endpoint de maior tráfego do serviço (`GET /api/jogos`, consulta por id). O padrão é
  ler do cache primeiro; em caso de *miss*, consulta o Postgres e popula o cache com TTL.
  Toda escrita (criação/atualização de jogo) invalida as chaves afetadas.
- **MongoDB — read model `game_catalog_extended`** — projeção desnormalizada do catálogo
  (dados de exibição/enriquecimento) otimizada para leitura em volume, sem sobrecarregar
  o Postgres com consultas analíticas/paginadas pesadas.
- **MongoDB — `processed_events`** — armazena o `MessageId` (envelope MassTransit) de
  cada evento já processado pelos consumers (ex.: `PaymentProcessedEvent`), garantindo
  idempotência em caso de redelivery do RabbitMQ.

### Chaves de cache (Redis)

| Chave                              | Conteúdo                        | TTL     |
|-------------------------------------|----------------------------------|---------|
| `catalog:jogos:list:ativos`        | Lista de jogos ativos (catálogo) | 5 min   |
| `catalog:jogo:{id}`                | Detalhe de um jogo por id        | 10 min  |

### Coleções e índices (MongoDB, banco `fcg_catalog`)

| Coleção                    | Índices                                   | Finalidade                    |
|-----------------------------|--------------------------------------------|--------------------------------|
| `game_catalog_extended`     | `{ jogoId: 1 }` (único), `{ ativo: 1 }`   | Read model do catálogo         |
| `processed_events`          | `{ messageId: 1 }` (único)                | Idempotência de consumers      |

### Comandos de validação

```bash
# Redis — chaves de cache do catálogo
redis-cli -h localhost -p 6379 KEYS 'catalog:*'

# MongoDB — índices da coleção de read model
mongosh fcg_catalog --eval "db.game_catalog_extended.getIndexes()"

# MongoDB — eventos já processados (idempotência)
mongosh fcg_catalog --eval "db.processed_events.find().limit(5)"
```

## Executar localmente

```bash
# PowerShell (Linux/macOS: cp .env.example .env)
Copy-Item .env.example .env
# edite o .env: POSTGRES_PASSWORD, RABBITMQ_PASSWORD e JWT_SECRET
# (JWT_SECRET igual ao do UsersAPI, mínimo 32 caracteres)

docker compose up -d
# Swagger: http://localhost:8082/swagger
```

O `docker compose` se recusa a subir se alguma dessas variáveis estiver vazia. O `.env` não é versionado.

## EF Core Migrations

```bash
cd src/FCG.CatalogAPI.API
dotnet ef migrations add InitialCreate \
  --project ../FCG.CatalogAPI.Infrastructure \
  --startup-project .
dotnet ef database update
```

## Deploy Kubernetes

**No AKS** o deploy é feito pelo pipeline (veja CI/CD) e os segredos vêm do Azure Key Vault. Os manifestos da plataforma ficam no repositório `fcg-orchestration`.

**Cluster local:** o Catalog depende de Postgres e RabbitMQ, que não estão neste repositório. O jeito mais simples é subir a plataforma inteira pelo `fcg-orchestration` (siga o README dele). Para testar só o Catalog localmente, use o `docker compose` da seção "Executar localmente".

Os manifestos deste repositório (`k8s/`) servem de referência. Para aplicá-los num cluster local que já tenha Postgres e RabbitMQ com as mesmas credenciais:

```bash
# 1. Construir a imagem (o deployment usa fcg-catalog-api:latest)
docker build -t fcg-catalog-api:latest .
# Cluster kind (Docker Desktop recente): kind load docker-image fcg-catalog-api:latest

# 2. Criar o Secret a partir do modelo (o secret.yaml é ignorado pelo Git)
Copy-Item k8s/secret.example.yaml k8s/secret.yaml
# troque cada <placeholder> por um valor seu

# 3. Aplicar os arquivos um a um
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/redis.yaml
kubectl apply -f k8s/mongo.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl get pods -n fcg
```

> Não use `kubectl apply -f k8s/`: isso aplicaria também o `secret.example.yaml`, com placeholders no lugar dos valores reais.
## Grupo 17 — Pos-Tech FIAP
- Letícia Lopes Ribeiro Vasconcelos
- Marcelo Henrique Cornelis Rei
- Washington Santana dos Santos
- Raul Hentz Rodrigues
