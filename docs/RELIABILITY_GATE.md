# TrueCompare Reliability Gate

Este validador é o teste final auditável de fiabilidade das recomendações da TrueCompare contra dados de mercado do KuantoKusta e uma referência obtida no ChatGPT visual.

## Regras obrigatórias

- Não usa OpenAI API, Azure OpenAI, OpenRouter, SDKs, `OPENAI_API_KEY`, `.env` ou secrets.
- A referência GPT live só é válida quando vem do ChatGPT no Chrome, em interface visual, com sessão Pro já autenticada.
- Se o ChatGPT pedir login, não deixar escolher modelo, não permitir conversa temporária/limpa ou não devolver resposta capturável, o caso fica `BLOQUEADO` ou `INCONCLUSIVO`.
- Os testes normais usam fixtures gravadas para serem repetíveis e não dependerem de internet.
- A validação live só corre quando flags e variáveis de ambiente explícitas estão ativas.

## Testes rápidos com fixtures

```powershell
dotnet test
```

Também podes gerar um relatório fixture manual:

```powershell
dotnet run --project tools/TrueCompare.ReliabilityGate -- --output artifacts/reliability-gate/fixture
```

Saídas geradas:

- `reliability-report.html`
- `reliability-report.json`
- `reliability-report.csv`

## Validação live parcial

Exemplo para uma categoria:

```powershell
$env:RUN_LIVE_RELIABILITY_GATE='true'
$env:RUN_LIVE_KUANTOKUSTA='true'
dotnet run --project tools/TrueCompare.ReliabilityGate -- --live --live-kuantokusta --category tecnologia --output artifacts/reliability-gate/live-tecnologia
```

Se o KuantoKusta bloquear browser automation com `Access Denied`, o relatório deve ficar `INCONCLUSIVO` com screenshots/snapshots de evidência.

## Validação live com ChatGPT visual

Para reutilizar o Chrome autenticado, arranca o Chrome com remote debugging e usa esse endpoint no validador:

```powershell
$env:RUN_LIVE_RELIABILITY_GATE='true'
$env:RUN_LIVE_KUANTOKUSTA='true'
$env:RUN_LIVE_CHATGPT_BROWSER_VALIDATION='true'
$env:CHATGPT_BROWSER_MODE='true'
dotnet run --project tools/TrueCompare.ReliabilityGate -- --live --live-kuantokusta --live-chatgpt --category tecnologia --chrome-cdp http://127.0.0.1:9222 --output artifacts/reliability-gate/live-chatgpt-tecnologia
```

O script abre `https://chatgpt.com/pt-PT/`, tenta usar conversa temporária ou conversa nova limpa, tenta confirmar o modelo pedido (`GPT-5.5`) e modo `Alta`, envia o prompt pela caixa de chat e guarda:

- prompt enviado;
- resposta completa;
- screenshot;
- timestamp;
- modelo/modo visível quando detetável.

## Critérios de veredito

O HTML inclui a secção obrigatória `Veredito final de fiabilidade` com um destes estados:

- `PASSOU: aplicação validada no teste final`
- `FALHOU: aplicação não validada`
- `INCONCLUSIVO: validação incompleta por bloqueios externos, páginas não acessíveis ou dados insuficientes`

A existência de qualquer falha crítica força `FALHOU`, mesmo que o score agregado seja alto.

Falhas críticas:

- produto inventado;
- produto fora da subcategoria;
- preço gravemente desalinhado;
- recomendação sem evidência;
- aplicação sem resposta validável para subcategoria com dados de mercado.

Thresholds:

- score agregado mínimo: `85%`;
- score mínimo por categoria: `75%`;
- `0` falhas críticas abertas.

## Estrutura

- `tools/TrueCompare.ReliabilityGate/Browser/kuantokusta-live.mjs`: coleta live KuantoKusta com rate limit e evidências.
- `tools/TrueCompare.ReliabilityGate/Browser/chatgpt-browser-reference.mjs`: automação visual do ChatGPT, sem API.
- `tools/TrueCompare.ReliabilityGate/Fixtures/kuantokusta-sample-fixture.json`: fixture estável para CI/testes rápidos.
- `tools/TrueCompare.ReliabilityGate/ReliabilityScorer.cs`: comparação, scoring e veredito.
- `tools/TrueCompare.ReliabilityGate/ReliabilityReportWriter.cs`: relatórios HTML/JSON/CSV.
