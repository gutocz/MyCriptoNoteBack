# MyCriptoNote Backend

API REST para o MyCriptoNote: gerenciamento de notas criptografadas com arquitetura Zero-Knowledge. Autenticação JWT, persistência em PostgreSQL e documentação Swagger.

## Stack

- **.NET 8** — ASP.NET Core Web API
- **PostgreSQL 16** — banco de dados
- **Entity Framework Core 8** — ORM
- **JWT** — autenticação
- **BCrypt** — hash de senhas
- **Swagger (Swashbuckle)** — documentação da API

## Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) e Docker Compose (para rodar tudo containerizado)
- Ou, para desenvolvimento local: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) e PostgreSQL 16

---

## Rodar do zero (Docker — recomendado)

Tudo sobe containerizado, com hot reload na API.

### 1. Configurar variáveis de ambiente

Na raiz do repositório (`MyCriptoNoteBack/`), crie o arquivo `.env` (ele não é versionado):

```env
POSTGRES_PASSWORD=postgres
JWT_KEY=SuperSecretKeyForDev_MustBeAtLeast32Chars!
```

Ajuste a senha e a chave JWT se quiser; em produção use valores seguros.

### 2. Subir os serviços

```bash
cd MyCriptoNoteBack
docker compose up --build
```

Isso sobe:

- **PostgreSQL** em `localhost:5433`
- **API** em `http://localhost:5116` (Swagger em `/swagger`)

A API usa hot reload: alterações em `.cs` são detectadas e a aplicação reinicia automaticamente.

### 3. Aplicar migrations (primeira vez ou banco novo)

Com os containers em execução, em outro terminal:

```bash
cd MyCriptoNoteBack/MyCriptoNote.API
dotnet ef database update
```

Para o EF encontrar o banco, a connection string precisa apontar para o Postgres na porta **5433** (ex.: em `appsettings.Development.json` ou User Secrets). Se estiver usando só Docker e não tiver .NET na máquina, aplique as migrations dentro do container:

```bash
docker exec -it mycriptonote-api dotnet ef database update
```

### 4. Parar os serviços

No terminal onde rodou `docker compose up`, use `Ctrl+C`. Para remover também os containers e volumes:

```bash
docker compose down
```

Para remover inclusive o volume do banco (apaga todos os dados):

```bash
docker compose down -v
```

---

## Rodar localmente (sem Docker)

Útil para desenvolver e debugar a API direto na máquina.

### 1. Subir o PostgreSQL

Se não quiser usar Docker para nada, instale o PostgreSQL e crie o banco `mycriptonote_dev`. Ou suba só o banco com Docker:

```bash
cd MyCriptoNoteBack
docker compose up -d postgres
```

### 2. Configurar segredos

Na pasta da API:

```bash
cd MyCriptoNote.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=mycriptonote_dev;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:Key" "MyCriptoNote-SuperSecretKey-ChangeInProduction-MinLength32Chars!"
```

Ajuste host, porta, usuário, senha e chave JWT conforme seu ambiente. Detalhes em [MyCriptoNote.API/SECRETS.md](MyCriptoNote.API/SECRETS.md).

### 3. Aplicar migrations

```bash
dotnet ef database update
```

### 4. Executar a API

```bash
dotnet run
```

A API fica em `http://localhost:5116`; Swagger em `http://localhost:5116/swagger`.

---

## Estrutura do repositório

```
MyCriptoNoteBack/
├── docker-compose.yml       # Postgres + API (rede mycriptonote-net)
├── .env                     # Variáveis para Docker (não versionado)
├── README.md
└── MyCriptoNote.API/
    ├── Dockerfile.dev       # Imagem de desenvolvimento com dotnet watch
    ├── Controllers/
    ├── Data/                # DbContext
    ├── DTOs/
    ├── Migrations/
    ├── Models/
    ├── Services/
    ├── Middleware/
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    └── SECRETS.md           # Guia de segredos (User Secrets / env)
```

---

## Integração com o frontend

O frontend (repositório separado) usa a rede Docker `mycriptonote-net` para falar com esta API. **Subir o backend primeiro** cria essa rede; depois o frontend faz `docker compose up` e acessa a API pelo nome do serviço `api`.

CORS está configurado para permitir `http://localhost:4200` (dev server do Angular).

---

## Comandos úteis

| Comando | Descrição |
|--------|-----------|
| `docker compose up --build` | Sobe Postgres + API com hot reload |
| `docker compose up -d` | Sobe em segundo plano |
| `docker compose down` | Para e remove containers |
| `dotnet ef database update` | Aplica migrations (local ou dentro do container) |
| `dotnet ef migrations add NomeDaMigracao` | Cria nova migration (na pasta MyCriptoNote.API) |

---

## Variáveis de ambiente (Docker)

O `docker-compose.yml` usa o `.env`. Principais variáveis:

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `POSTGRES_PASSWORD` | Senha do usuário `postgres` | `postgres` |
| `JWT_KEY` | Chave secreta JWT (mín. 32 caracteres) | (valor de exemplo no compose) |

A connection string da API é montada automaticamente com o host `postgres` e a senha de `POSTGRES_PASSWORD`.
