# TrueCompare

TrueCompare e uma Blazor Web App em .NET 10 para comparar produtos, validar lojas/precos e criar alertas de preco.

A aplicacao deve ajudar o utilizador a procurar qualquer produto por texto ou imagem, sugerir opcoes reais do mercado, validar lojas com URL direta do produto e enviar alertas quando o preco alvo for atingido.

## Documentacao principal

- [Blueprint da aplicacao](docs/TRUECOMPARE_BLUEPRINT.md): visao do produto, regras de negocio, mercados, lojas, ofertas validas, LLM, alertas e roadmap.
- [Testes e qualidade](docs/TESTING_AND_QUALITY.md): checklist funcional, matriz de pesquisas, Playwright, testes .NET, seguranca, acessibilidade e criterios de pronto.
- [AGENTS.md](AGENTS.md): regras curtas para Codex e agentes de IA.
- [Melhorias aplicadas](docs/IMPROVEMENTS_APPLIED.md): resumo das alteracoes deste pacote.

## Stack

- .NET 10
- Blazor Web App
- Interactive Server render mode
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- HTML/CSS e componentes Razor nativos

## Base de dados

Em desenvolvimento, a aplicacao usa SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLExpress;Database=TrueCompareDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

As migrations devem ser aplicadas com EF Core. Nao usar `EnsureCreated()`.

Comandos uteis:

```powershell
dotnet tool run dotnet-ef migrations add NomeDaMigration --output-dir Data\Migrations
dotnet tool run dotnet-ef database update
```

## Configuracao local

Secrets devem ficar em User Secrets ou variaveis de ambiente. Nao colocar chaves reais no repositorio.

Exemplos:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."

dotnet user-secrets set "Stripe:SecretKey" "..."
dotnet user-secrets set "Stripe:WebhookSecret" "..."

dotnet user-secrets set "Email:Host" "smtp.exemplo.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:UserName" "..."
dotnet user-secrets set "Email:Password" "..."
dotnet user-secrets set "Email:FromEmail" "alerts@truecompare.pt"
```

## Configuracao de producao

Define a origem publica usada por Stripe e redirects externos:

```json
"App": {
  "PublicBaseUrl": "https://truecompare.pt"
}
```

Ajusta tambem `AllowedHosts` para os dominios reais da instalacao. Em desenvolvimento, `appsettings.Development.json` usa `https://localhost:5001`.

## LLM

O LLM local/Ollama deve ser a primeira opcao quando estiver disponivel. Fallbacks externos so devem ser usados quando o local falhar ou nao suportar a tarefa.

Exemplo de configuracao:

```json
"Llm": {
  "Endpoint": "http://172.20.10.55:11434/v1/chat/completions",
  "Model": "qwen3-vl:235b-cloud"
}
```

## Testes

Para alteracoes pequenas, correr apenas testes focados. Para alteracoes de negocio, seguranca, dados, alertas, autenticacao ou refactors largos, correr a suite relevante e depois a suite completa quando fizer sentido.

```powershell
dotnet test TrueCompare.slnx
```

Os criterios completos estao em [docs/TESTING_AND_QUALITY.md](docs/TESTING_AND_QUALITY.md).

## Alertas de preco

Alertas devem ser enviados apenas quando existir preco confirmado, vendedor conhecido quando aplicavel e URL direta do produto. O email deve usar template HTML coerente com a aplicacao e nao deve incluir assinaturas automaticas indevidas.
