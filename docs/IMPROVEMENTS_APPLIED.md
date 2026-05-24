# Melhorias aplicadas

Este pacote inclui melhorias incrementais focadas em segurança, confiança dos preços, quota justa, proteção de operações caras e robustez operacional.

## Segurança e configuração

- `BillingController` deixou de construir URLs do Stripe apenas a partir do `Host` recebido no pedido.
- Nova opção `App:PublicBaseUrl` para definir a origem pública usada nos checkouts Stripe.
- `AllowedHosts` deixou de estar como wildcard no ficheiro base e o ficheiro de produção já não inclui `localhost`/`127.0.0.1`.
- Configuração local de LLM com IP interno foi movida para `appsettings.Development.json`; o `appsettings.json` base ficou sem endpoints internos.
- Foram adicionados `ForwardedHeaders` para suportar reverse proxy/load balancer e melhorar a fiabilidade do IP usado em rate limiting.
- A Content Security Policy passou a ser diferente por ambiente: desenvolvimento/teste permissivo, produção mais restritiva.
- Rate limiting passou a ser particionado por utilizador autenticado ou IP, reduzindo o risco de um utilizador afetar todos os outros.
- Foram adicionadas políticas específicas para `search`, `alerts`, `billing`, `auth` e `image-analysis`.
- Registo, alteração de email e alteração de password receberam validações mais fortes.
- Foram adicionados envio de link de confirmação, reenvio quando o login é bloqueado por email não confirmado e endpoint `/auth/confirm-email`.
- O login Google passa a mapear/validar `email_verified` quando a confirmação de email está ativa.
- Em produção, `Security:RequireConfirmedEmail`, `AutoConfirmLocalAccounts=false` e `AutoConfirmEmailChanges=false` ficaram como defaults seguros no ficheiro base.

## Quota, idempotência e resultados

- `/results` exige autenticação com `[Authorize]`, fechando o bypass direto por URL.
- `/checkout` também passou a exigir autenticação, porque valida lojas e pode preparar alertas.
- `ProductDetail` deixou de chamar descoberta LLM diretamente. A página pública só usa catálogo local ou snapshots já gerados por uma pesquisa autenticada.
- A página inicial deixou de consumir quota antes de navegar; agora gera um `requestId` e a reserva/commit/refund acontece no carregamento real dos resultados.
- `SearchQuotaService` deixou de conceder créditos ilimitados com base no `Host` recebido. O modo ilimitado local só ativa quando `SearchQuota:LocalUnlimitedEnabled=true` e o ambiente é `Development` ou `Testing`.
- `SearchQuotaService` suporta reservas idempotentes de quota ligadas à query:
  - `Pending` enquanto a pesquisa está a correr;
  - `Committed` quando há resultados úteis;
  - `Refunded` quando a pesquisa falha antes de gerar valor útil.
- As reservas guardam `IdempotencyKey`, `ResultCount`, `CommittedUtc`, `RefundedUtc`, `FailureReason` e se a pesquisa usou crédito pago ou pesquisa grátis.
- Novo `SearchOrchestratorService` centraliza autenticação lógica, reserva de quota, descoberta, validação de lojas, commit/refund e snapshots.
- Novo `SearchResultSnapshotService` e tabela `SearchResultSnapshots` guardam resultados durante algumas horas para evitar repetir LLM/validação HTTP em refresh, reconexão ou reabertura da mesma pesquisa.
- O histórico lista apenas pesquisas `Committed`, evitando mostrar tentativas devolvidas/falhadas.
- Os resultados distinguem produto encontrado de oferta live-validada: produtos podem aparecer mesmo quando a validação live da loja falha, mas a UI informa que o preço está por confirmar.

## Proteção de operações caras

- A análise de imagem passou a exigir utilizador autenticado.
- A análise de imagem usa rate limiting in-memory por utilizador e também reserva de quota.
- Quando a análise de imagem não gera sugestão útil, a reserva é devolvida.
- `/product/{slug}?query=...` e `/checkout?query=...` deixaram de acionar descoberta LLM fora do fluxo controlado de `/results`.
- `/checkout` reutiliza snapshot quando existe; se não existir, só usa catálogo local e validação das ofertas conhecidas do produto.

## Preços, lojas e alertas

- As ofertas expõem `ValidationState`, `ValidatedUtc` e `IsLiveValidated`.
- A validação live de lojas marca explicitamente ofertas como `LiveValidated`.
- A criação de alertas tenta validar live a oferta no momento da criação.
- Quando a criação do alerta não consegue validação live, o alerta fica como `PendingValidation` e o monitor revalida antes de enviar qualquer email.
- Alertas com `ProductSlug` inválido passaram a ser rejeitados; já não há fallback silencioso para o primeiro produto do catálogo.
- Foi adicionado limite configurável de alertas ativos por utilizador (`PriceAlerts:MaxActiveAlertsPerUser`).
- Alertas guardam metadata da última validação (`LastValidationState` e `LastValidatedUtc`).
- O monitor de alertas agrupa alertas por produto, valida uma vez por `ProductSlug` e aplica o resultado a todos os alertas desse produto.
- Alertas já não enviam email com base apenas num preço de catálogo/conhecido: o monitor exige oferta `LiveValidated` antes de enviar.
- O envio de emails de alerta ficou isolado por alerta, com `EmailFailureCount`, `LastEmailAttemptUtc`, `LastEmailError`, `NextEmailRetryUtc` e `EmailSuppressedUtc`.
- Falhas de email usam backoff exponencial configurável; depois de várias falhas, o alerta é suspenso em vez de tentar enviar indefinidamente.
- `SmtpEmailSender` devolve `false` em falhas SMTP não canceladas, registando logs em vez de propagar exceções para todo o ciclo.

## Validação externa de lojas

- A validação de ofertas limita concorrência por produto.
- A validação de ofertas tem também limite global de concorrência e backoff curto por domínio quando uma loja falha/timeout.
- Foi adicionado limite por domínio por minuto (`MaxRequestsPerDomainPerMinute`) para reduzir pressão sobre lojas externas.
- A validação tem timeout configurável.
- O número de ofertas validadas por produto e o tamanho máximo da resposta HTTP são configuráveis.
- Respostas demasiado grandes são rejeitadas antes de processar o HTML.
- As respostas são lidas com limite de bytes para evitar consumo excessivo de memória.

## LLM

- Providers locais também entram em cooldown quando devolvem rate limit (`429`), evitando chamadas repetidas durante o período de espera.
- Chamadas paralelas a modelos locais aproveitam até duas respostas válidas para validação cruzada e cancelam as restantes, reduzindo latência sem perder diversidade quando há respostas rápidas.
- O LLM de descoberta agora só é chamado pelo `SearchOrchestratorService`, não por páginas públicas avulsas.

## Mercado/localização

- Idioma inglês deixou de implicar automaticamente mercado dos EUA.
- Apenas culturas específicas dos EUA usam mercado `US`; as restantes fazem fallback seguro para `PT`.

## Imagem

- O upload/colar imagem valida content type, extensão e assinatura/magic bytes para PNG, JPEG e WebP.
- O upload/análise de imagem deixou de chamar o serviço de análise duas vezes para a mesma imagem.
- A análise de imagem exige login, rate limit e reserva de quota.

## Configurações novas ou relevantes

```json
{
  "App": {
    "PublicBaseUrl": "https://truecompare.pt"
  },
  "Security": {
    "RequireConfirmedEmail": true,
    "AutoConfirmLocalAccounts": false,
    "AutoConfirmEmailChanges": false
  },
  "SearchQuota": {
    "LocalUnlimitedEnabled": false
  },
  "PriceAlerts": {
    "MaxActiveAlertsPerUser": 5,
    "MaxEmailFailures": 5,
    "FirstEmailRetryMinutes": 15,
    "MaxEmailRetryHours": 24
  },
  "StoreOfferValidation": {
    "RequestTimeoutSeconds": 8,
    "MaxConcurrentRequests": 3,
    "MaxGlobalConcurrentRequests": 8,
    "MaxRequestsPerDomainPerMinute": 30,
    "DomainBackoffSeconds": 15,
    "MaxOffersPerProduct": 6,
    "MaxResponseBytes": 1000000
  }
}
```

Em produção, define `App:PublicBaseUrl` com HTTPS, ajusta `AllowedHosts` aos domínios reais e configura SMTP/Stripe/LLM por variáveis de ambiente, User Secrets ou outro secret provider.

## Migrations novas nesta ronda

- `20260523184000_AddSearchResultSnapshotsAndAlertEmailRetry.cs`

Esta migration adiciona:

- tabela `SearchResultSnapshots`;
- `NextEmailRetryUtc` em `TargetPriceAlerts`;
- `EmailSuppressedUtc` em `TargetPriceAlerts`.

## Validação feita neste ambiente

- `appsettings.json` e `appsettings.Development.json` foram mantidos como JSON válido.
- O pacote final foi limpo de `bin`, `obj`, `node_modules`, `.codex-run`, `test-results`, `artifacts` e ficheiros `.user`.
- Não foi possível compilar/executar testes aqui porque o SDK `dotnet` não está instalado neste ambiente.

---

# Ronda adicional v3

## Pesquisa e quota

- `/results` deixou de executar uma pesquisa por defeito quando não existe `query` explícita.
- Abrir `/results` sem query mostra estado vazio e não reserva, consome nem valida nada.
- Quando `/results?query=...` é aberto sem `requestId`, a página redireciona para a mesma pesquisa com `requestId` novo antes de executar qualquer operação cara.
- Links internos que antes apontavam para `/results` sem query foram corrigidos para `/` ou para páginas de produto explícitas.
- `SearchOrchestratorService` passou a guardar o snapshot antes do commit definitivo da quota.
- Se a pesquisa gera resultados mas o snapshot falha antes do commit, a reserva é devolvida e a UI pode mostrar os resultados sem debitar o utilizador.
- Novo `SearchMaintenanceService` devolve automaticamente reservas `Pending` expiradas e apaga snapshots expirados.
- `SearchQuota` ganhou opções configuráveis:
  - `PendingReservationExpiryMinutes`;
  - `SnapshotTtlHours`;
  - `SnapshotCleanupIntervalHours`.

## LLM

- `LlmProviderRouter.ResolveApiKey` deixou de usar `OPENAI_API_KEY`/`TRUECOMPARE_LLM_API_KEY` como fallback universal para providers que têm variável específica, como Gemini, Groq ou OpenRouter.
- O fallback genérico só fica disponível para providers sem variável específica, para `TRUECOMPARE_LLM_API_KEY`, ou para providers OpenAI explícitos.
- Respostas paralelas locais agora devolvem a primeira resposta válida para operações rápidas; a validação cruzada continua reservada para operações de descoberta.

## Conta e autenticação

- Login Google passou a exigir `email_verified == true` antes de iniciar sessão quando a confirmação de email está ativa.
- Adicionado fluxo de recuperação de password:
  - `/forgot-password`;
  - `/reset-password`;
  - `/auth/forgot-password`;
  - `/auth/reset-password`.
- O pedido de recuperação de password usa mensagem genérica para não revelar se o email existe.
- A alteração de email passou a usar token de mudança de email quando `AutoConfirmEmailChanges=false`, mantendo o email antigo ativo até confirmação do novo.
- Adicionado endpoint `/auth/confirm-email-change`.

## Checkout, Stripe e billing

- A abertura direta de `/checkout` sem produto/query já não valida automaticamente um produto por defeito.
- Webhooks Stripe passaram a ter limite de tamanho de body.
- Adicionada tabela `StripeProcessedEvents` para idempotência por `Event.Id`.
- `BillingService` passou a tratar eventos repetidos, falhados e processados.
- `BillingService` passou a guardar `StripeCustomerId` no utilizador quando disponível.
- Foram adicionados tratamentos para:
  - `invoice.payment_failed`;
  - `customer.subscription.updated`;
  - `customer.subscription.deleted`.

## Validação de lojas

- Validação de ofertas rejeita URLs sem `http`/`https`, `localhost`, loopback e IPs privados/link-local.
- O `HttpClientHandler` de validação de lojas tem `AllowAutoRedirect=false` em produção.
- A validação rejeita respostas cujo destino final tenha host inesperado.

## Migration nova

- `20260524101000_AddStripeProcessedEventsAndCustomerId.cs`

Esta migration adiciona:

- coluna `StripeCustomerId` em `AspNetUsers`;
- tabela `StripeProcessedEvents`;
- índices para `StripeCustomerId`, `StripeEventId` e estado/data dos eventos Stripe.

---

# Ronda focada em pesquisa, sugestões de produtos e lojas

## Pesquisa e matching de produtos

- O prompt de descoberta de produtos passou a pedir 2 a 4 produtos reais, ordenados por correspondência de categoria/modelo, adequação ao uso, preço plausível, garantia e diversidade de marcas.
- A descoberta passou a rejeitar mais agressivamente respostas de categoria errada, por exemplo:
  - carregador de iPhone não deve devolver iPhone;
  - microfone não deve devolver portátil;
  - cadeira não deve devolver consola;
  - impressora não deve devolver smartphone;
  - fraldas não devem devolver ração.
- O ranking local passou a dar mais peso a intenção explícita do utilizador, incluindo orçamento, gaming, trabalho, bateria, câmara, silêncio, garantia/oficial e barato/baixo custo.
- Foram adicionados fallbacks de categoria mais úteis para pedidos que não existem no catálogo principal, incluindo microfones e mesas/secretárias.
- `LlmProductDiscoveryService.GetSellerOffersAsync` passou a usar o produto local com o texto original da pesquisa para ordenar lojas de acordo com a intenção do utilizador, em vez de devolver sempre lojas genéricas do slug.

## Sugestões na página de resultados

- A página `/results` passou a mostrar pesquisas sugeridas clicáveis, baseadas no pedido atual.
- A página `/results` passou a mostrar leads/alternativas de produto com motivo e link para nova pesquisa.
- Cada produto passou a mostrar uma linha curta com as lojas sugeridas mais relevantes.
- A mensagem de validação distingue melhor preço live-validado, lojas conhecidas pendentes e produto encontrado sem preço confirmado.

## Lojas sugeridas

- `ComparisonDataService.BuildSellerOffersForProduct` passou a devolver não só ofertas diretas conhecidas, mas também pesquisas úteis em lojas relevantes quando não existe URL direta validada.
- As lojas agora são pontuadas por:
  - fiabilidade da loja;
  - existência de página direta conhecida;
  - validação live quando disponível;
  - especialização por categoria;
  - loja oficial da marca quando aplicável;
  - intenção do utilizador, como “barato”, “garantia” ou “oficial”.
- Adicionado `GetSuggestedStoreSearches`, para checkout e detalhe de produto mostrarem lojas úteis mesmo quando não existe preço live validado.
- A página de detalhe de produto passou a mostrar lojas sugeridas para continuar a pesquisa.
- O checkout passou a mostrar pesquisas externas em lojas adequadas quando ainda não existe oferta live-validada, evitando cair sempre em KuantoKusta.
- Adicionada `PcComponentes` ao conjunto de lojas PT para informática, smartphones, periféricos, monitores, armazenamento, TV/câmara e tecnologia geral.
- Reforçadas especializações por categoria:
  - animais: Tiendanimal, Continente e KuantoKusta;
  - bricolage/jardim: Leroy Merlin, Worten, KuantoKusta e Amazon;
  - desporto: Decathlon, KuantoKusta e Amazon;
  - casa/mobiliário: IKEA, Leroy Merlin, Worten e Amazon;
  - auto/pneus: Norauto, KuantoKusta e Amazon;
  - saúde, beleza e bebé: Wells, Continente, Worten, KuantoKusta e Amazon;
  - escritório/impressão: Staples, FNAC, Worten, PCDIGA e Globaldata;
  - tecnologia/audio/smartphones: Worten, FNAC, PcComponentes, PCDIGA, Globaldata e lojas oficiais quando aplicável.
- O checkout passou a mostrar evidência/contexto por loja, por exemplo página direta conhecida, comparador útil ou loja recomendada por categoria.

## Testes adicionados/atualizados

- Teste para garantir que produtos sem página direta continuam a devolver lojas úteis, como Worten, FNAC, PcComponentes ou KuantoKusta.
- Teste para garantir que pesquisas de smartphone barato não são contaminadas por produtos de outra categoria.
- Teste atualizado para aceitar ofertas sugeridas/lojas de pesquisa em vez de exigir apenas oferta direta confirmada.

## Nota de validação

- O ZIP foi validado estruturalmente com `unzip -t` depois de empacotado.
- Não foi possível compilar/executar testes neste ambiente porque o SDK `dotnet` não está instalado.
