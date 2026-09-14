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
| `Jwt__Issuer`                   | `FCG.UsersAPI`                     |
| `Jwt__Audience`                 | `FCG.Platform`                     |
| `RabbitMQ__Host`                | Host do RabbitMQ                   |
| `RabbitMQ__Password`            | Senha RabbitMQ *(via Secret)*      |
| `Redis__ConnectionString`       | String de conexão Redis (`host:porta`) |
| `Mongo__ConnectionString`       | String de conexão MongoDB *(via Secret)* |
| `Mongo__Database`               | `fcg_catalog`                      |

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
docker compose up -d
# Swagger: http://localhost:8082/swagger
```

## EF Core Migrations

```bash
cd src/FCG.CatalogAPI.API
dotnet ef migrations add InitialCreate \
  --project ../FCG.CatalogAPI.Infrastructure \
  --startup-project .
dotnet ef database update
```

## Deploy Kubernetes

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/redis.yaml
kubectl apply -f k8s/mongo.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```
## Grupo 17 — Pos-Tech FIAP
- Letícia Lopes Ribeiro Vasconcelos
- Marcelo Henrique Cornelis Rei
- Washington Santana dos Santos
- Raul Hentz Rodrigues
