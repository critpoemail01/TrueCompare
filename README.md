# TrueCompare

Blazor Web App em .NET 8 com Identity, SQL Server, limite de pesquisas e pagamentos por cartão via Stripe Checkout.

## Princípio de UX

A aplicação deve falar por si: ações claras, estados visíveis e o mínimo de texto explicativo no ecrã.

## Segurança e arquitetura de dados

- Blazor Web App em .NET 8 com renderização interativa server-side.
- ASP.NET Core Identity guarda utilizadores e hashes de password em SQL Server.
- Passwords exigem mínimo de 10 caracteres, maiúscula, minúscula, número e símbolo.
- Lockout ativo após tentativas falhadas e cookies de autenticação `HttpOnly`, `SameSite=Lax` e seguros em produção.
- POSTs MVC usam validação antiforgery por omissão; o webhook Stripe é a exceção controlada.
- Endpoints de login, pagamento e alertas usam rate limiting.
- Compras e consumo de créditos correm em transações para evitar dupla atribuição ou consumo concorrente.
- Cartões são processados pelo Stripe Checkout; a aplicação não guarda dados de cartão.
- Secrets ficam em user-secrets/variáveis de ambiente, não em código.

## Base de dados

A aplicação usa SQL Server Express por omissão:

```json
"DefaultConnection": "Server=.\\SQLExpress;Database=TrueCompareDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Se a instância exigir `sa`, troca a connection string por:

```json
"DefaultConnection": "Server=.\\SQLExpress;Database=TrueCompareDb;User Id=sa;Password=A_TUA_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Comandos usados:

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate --output-dir Data\Migrations
dotnet tool run dotnet-ef database update
```

## Login Google

Configura OAuth no Google Cloud Console e define os secrets localmente:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
```

Callback OAuth:

```text
https://localhost:7022/signin-google
http://localhost:5241/signin-google
```

## Pagamentos

Os cartões são tratados pelo Stripe Checkout. A app não guarda dados de cartão.

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

Webhook:

```text
POST /billing/stripe-webhook
```

Evento necessário:

```text
checkout.session.completed
```

## LLM de sugestões

As sugestões aparecem nos resultados e correm no servidor. A chave nunca é enviada para o browser. Sem chave configurada, a app usa o catálogo local como fallback.

```powershell
dotnet user-secrets set "Llm:ApiKey" "sk-..."
dotnet user-secrets set "Llm:Model" "gpt-4o-mini"
```

Também podes usar variável de ambiente:

```powershell
$env:TRUECOMPARE_LLM_API_KEY="sk-..."
```

Endpoint por omissão:

```json
"Llm": {
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "Model": "gpt-4o-mini"
}
```

## Regra de créditos

- Cada utilizador tem 3 pesquisas gratuitas.
- Depois disso, cada comparação consome 1 crédito.
- A compra Stripe cria um registo em `CreditPurchases`.
- O webhook confirmado adiciona os créditos ao utilizador.

## Alertas de preço por email

Configura SMTP para enviar alertas quando o melhor preço conhecido ficar abaixo do preço alvo:

```powershell
dotnet user-secrets set "Email:Host" "smtp.exemplo.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:UserName" "..."
dotnet user-secrets set "Email:Password" "..."
dotnet user-secrets set "Email:FromEmail" "alerts@truecompare.pt"
```

O monitor corre em background a cada 15 minutos e verifica alertas ativos em `TargetPriceAlerts`.
