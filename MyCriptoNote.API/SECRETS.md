# Configuração de segredos (desenvolvimento)

Os arquivos `appsettings.json` e `appsettings.Development.json` contêm apenas **placeholders**. Os valores reais devem vir de **User Secrets** (dev) ou **variáveis de ambiente** (produção).

## Primeira vez (desenvolvimento local)

Na pasta do projeto da API:

```bash
cd MyCriptoNoteBack/MyCriptoNote.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=mycriptonote_dev;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:Key" "MyCriptoNote-SuperSecretKey-ChangeInProduction-MinLength32Chars!"
```

Substitua a connection string e a chave JWT pelos seus valores locais, se forem diferentes.

## Produção

Use variáveis de ambiente (ou um provedor de segredos, ex.: Azure Key Vault):

- `ConnectionStrings__DefaultConnection` — connection string do banco
- `Jwt__Key` — chave JWT com no mínimo 32 caracteres

## Docker

Para definir a senha do Postgres no `docker-compose`:

```bash
export POSTGRES_PASSWORD=sua_senha_segura
docker compose up -d
```

No Windows (PowerShell): `$env:POSTGRES_PASSWORD="sua_senha_segura"; docker compose up -d`

Se não definir, o padrão local é `postgres` (apenas para desenvolvimento).
