# TrueCompare - Relatorio de Conformidade

Data: 2026-05-22

Este documento resume os pontos de conformidade aplicados ao projeto sem alterar o README.

## Estado validado

- A aplicacao corre em Blazor Web App com renderizacao interativa Server.
- A base de dados usa EF Core e migracoes; nao existe `EnsureCreated`.
- Componentes Razor nao acedem diretamente ao `ApplicationDbContext`.
- Controladores nao acedem diretamente ao `ApplicationDbContext`.
- Ofertas de loja so entram no checkout quando cumprem:
  - preco confirmado;
  - URL direta de produto;
  - vendedor identificado;
  - mercado coerente;
  - validacao de conteudo da pagina da loja.
- Produtos sem loja validada nao aparecem como compra recomendada no fluxo de resultados.
- Checkout sem oferta confirmada mostra estado vazio claro.
- Fontes oficiais de produto so aparecem quando existe pagina direta conhecida; pesquisas de marca/Google nao sao apresentadas como fonte oficial.
- Alertas de preco so podem ser criados para produtos com loja validada.
- Emails de alerta usam DTO proprio e nao recebem entidades EF.
- LLM nao fornece lojas/precos como verdade factual; ofertas vindas do LLM nao sao tratadas como confirmadas.
- Upload de imagem aceita apenas `.png`, `.jpg`, `.jpeg` e `.webp`, mostra preview, permite remover e bloqueia formatos invalidos.
- As sugestoes de categorias da home sao curadas: cada sugestao deve devolver produtos coerentes e chegar a checkout com pelo menos uma loja validada.
- A cobertura de categorias PT inclui as categorias oficiais de KuantoKusta, Worten e atalhos visiveis da home Worten mapeados para familias de produto com oferta validada.
- Categorias genericas como Gaming, Recondicionados, Mobilidade, Livros/Musica/Filmes, TV/Som e Eletrodomesticos filtram subfamilias para evitar artigos incoerentes.
- Playwright cobre pesquisa anonima, checkout sem loja, acesso protegido, upload/remocao de imagem, imagem invalida, categorias sugeridas e overflow mobile.

## Gates executados

- `dotnet test .\TrueCompare.Tests\TrueCompare.Tests.csproj --no-restore`
- `dotnet build .\TrueCompare.slnx --no-restore`
- `npx playwright test`
- `dotnet test .\TrueCompare.Tests\TrueCompare.Tests.csproj --no-restore --filter "FullyQualifiedName~ComparisonDataServiceTests"`
- `dotnet test .\TrueCompare.Tests\TrueCompare.Tests.csproj --no-restore --filter "FullyQualifiedName~StoreOfferValidationServiceTests|FullyQualifiedName~AppSmokeTests"`
- Scan de codigo para:
  - `EnsureCreated`
  - `MarkupString`
  - tenants
  - bibliotecas UI proibidas
  - segredos/connection strings sensiveis fornecidas na conversa
  - acesso a `DbContext` em `Controllers` e `Components`
  - geradores antigos de URLs de pesquisa para fonte oficial/LLM

## Regras criticas protegidas

- A UI nao deve inventar lojas, precos ou links.
- Uma URL de pesquisa nunca deve ser tratada como pagina direta de compra.
- O preco mostrado no checkout deve vir da oferta validada.
- O utilizador deve conseguir distinguir comprar agora de criar alerta.
- Se nao houver oferta validada, a aplicacao deve dizer isso claramente.
- Ficheiros de imagem invalidos ou grandes nao podem quebrar layout nem pesquisa.
- Uma categoria sugerida na home nao deve existir se nao conseguir levar a produto + detalhe + condicoes de compra com loja validada.
- Fluxos protegidos redirecionam utilizadores anonimos para login.

## App local

Ambiente atual: `Testing`

URL local validada: `http://localhost:5190`
