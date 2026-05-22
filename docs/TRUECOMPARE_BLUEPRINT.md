# TrueCompare - Blueprint da aplicacao

Ultima atualizacao: 2026-05-22

Este documento define a direcao funcional, tecnica e de dados da aplicacao TrueCompare. Deve ser tratado como referencia viva: sempre que a visao do produto, regras de negocio, fontes de dados, fluxos principais ou arquitetura mudarem, este ficheiro deve ser atualizado.

A checklist operacional de testes e validacao fica em `docs/TESTING_AND_QUALITY.md`.

## 1. Visao do produto

O TrueCompare deve permitir que um utilizador procure qualquer produto por texto ou imagem e receba recomendacoes validas, explicaveis e acionaveis.

A aplicacao deve:

- Entender o que o utilizador quer comprar.
- Identificar a categoria correta do produto.
- Respeitar limites explicitos como preco maximo, tamanho, marca, autonomia, capacidade, compatibilidade, localizacao e urgencia.
- Sugerir as melhores opcoes reais do mercado.
- Mostrar lojas validas onde o produto pode ser comprado.
- Confirmar preco, vendedor, URL do produto, entrega, garantia e confianca antes de recomendar uma loja.
- Permitir abrir a pagina real do produto na loja.
- Permitir criar alertas de preco.
- Monitorizar os alertas e enviar email apenas quando existir preco confirmado.

O objetivo nao e mostrar muitos resultados. O objetivo e mostrar poucos resultados bons, corretos e comprovaveis.

## 2. Principios inegociaveis

- A aplicacao nao deve inventar produtos, precos, lojas, vendedores ou URLs.
- Um produto sugerido deve pertencer a categoria pretendida pelo utilizador.
- Um limite de preco explicito deve ser respeitado.
- Uma loja so deve ser apresentada como opcao valida quando existir URL direto do produto e preco confirmado.
- URLs de pesquisa nao devem ser tratados como URLs de produto.
- Se o preco final so puder ser confirmado na loja, a UI deve dizer isso claramente e nao apresentar um preco falso.
- O LLM ajuda a interpretar, resumir e comparar. A verdade factual vem de fontes estruturadas, conectores, paginas de produto e validadores.
- Cada pais suportado deve mostrar lojas, marketplaces, moeda, impostos, garantia e idioma coerentes com esse mercado.
- Portugal e EUA sao exemplos de mercados, nao limites da aplicacao.
- A lingua da aplicacao deve seguir a lingua do browser quando suportada.
- Uma resposta incompleta deve ser marcada como incompleta, nao mascarada como valida.

## 3. Jornada principal do utilizador

1. O utilizador procura um produto por texto ou imagem.
2. A aplicacao interpreta intencao, categoria, restricoes e mercado.
3. A aplicacao descobre produtos candidatos dentro da categoria correta.
4. A aplicacao descobre ofertas reais em lojas relevantes para o mercado do utilizador.
5. Cada oferta e validada antes de entrar no ranking.
6. A aplicacao ordena os produtos e lojas por adequacao, preco, confianca, entrega, garantia e disponibilidade.
7. O utilizador abre detalhes, compara opcoes e escolhe entre comprar agora ou criar alerta de preco.
8. Se criar alerta, a aplicacao monitoriza recorrente e envia email quando o preco alvo for atingido.

Estados obrigatorios da pesquisa:

- `IntentDetected`: a aplicacao entendeu o pedido do utilizador.
- `CategoryMatched`: a categoria foi identificada com confianca suficiente.
- `NeedsClarification`: faltam dados essenciais ou existe ambiguidade.
- `ProductsFound`: existem produtos candidatos dentro da categoria correta.
- `OffersFound`: existem ofertas encontradas em lojas relevantes.
- `OffersValidated`: existem ofertas com URL direto, preco confirmado e vendedor conhecido.
- `RankingGenerated`: existe recomendacao final explicavel.
- `NoValidOffers`: foram encontradas hipoteses, mas nenhuma passou a validacao.

Regra de clarificacao:

- Se o pedido for ambiguo, a aplicacao deve perguntar antes de recomendar.
- Se o utilizador pedir um acessorio, a aplicacao nao deve sugerir o produto principal.
- Exemplo: "carregador de iPhone" deve procurar carregadores/cabos/MagSafe, nao iPhones.
- Exemplo: "disco externo ate 20 euros" nao deve sugerir discos acima de 20 euros.

## 4. Modelo de dominio recomendado

Entidades principais:

- `Product`: produto normalizado apresentado ao utilizador.
- `ProductIdentity`: identificadores canonicos como marca, modelo, EAN, SKU, MPN e variantes.
- `Category`: categoria de produto e hierarquia.
- `ProductAttribute`: atributos estruturados usados para comparacao.
- `Store`: plataforma ou loja onde pode existir oferta.
- `Seller`: vendedor real, incluindo vendedores dentro de marketplaces.
- `Offer`: oferta atual validada para um produto numa loja/vendedor.
- `OfferSnapshot`: historico de preco, disponibilidade e validacao.
- `ValidationRun`: resultado de uma tentativa de validacao de produto/oferta.
- `PriceAlert`: alerta criado pelo utilizador.
- `SearchHistory`: historico de pesquisas.
- `UserMarketPreference`: mercado, moeda e localizacao preferencial.
- `SearchQuota`: plano, creditos e limites de utilizacao.

Contrato tecnico minimo de uma oferta valida:

| Campo | Obrigatorio | Regra |
| --- | --- | --- |
| `ProductName` | Sim | Nome do produto encontrado na fonte. |
| `Brand` | Sim, quando aplicavel | Marca normalizada. |
| `Model` | Sim, quando aplicavel | Modelo ou variante. |
| `Category` | Sim | Tem de coincidir com a categoria pretendida. |
| `StoreName` | Sim | Loja/plataforma. |
| `SellerName` | Sim para marketplace | Vendedor real da oferta. |
| `Country` | Sim | Mercado onde a oferta e valida. |
| `Currency` | Sim | Moeda do preco confirmado. |
| `ConfirmedPrice` | Sim | Preco final visivel na pagina ou fonte validada. |
| `ProductUrl` | Sim | URL direto da pagina do produto, nunca URL de pesquisa. |
| `Availability` | Sim | Em stock, indisponivel ou desconhecido. |
| `DeliveryEstimate` | Nao | Prazo quando a fonte o disponibiliza. |
| `Warranty` | Nao | Garantia quando a fonte a disponibiliza. |
| `ValidatedAt` | Sim | Data/hora UTC da validacao. |
| `ProductMatchScore` | Sim | Confianca de correspondencia produto-oferta. |
| `OfferConfidenceScore` | Sim | Confianca global da oferta. |
| `ValidationStatus` | Sim | `Valid`, `Rejected`, `NeedsReview` ou `Stale`. |

## 5. Arquitetura funcional

Camadas esperadas:

- `Web`: Blazor UI, paginas, componentes, localizacao visual, fluxos de utilizador.
- `Application`: casos de uso, DTOs, validacoes, orquestracao de pesquisa, ranking e alertas.
- `Domain`: entidades, value objects, regras de negocio e invariantes.
- `Infrastructure`: EF Core, Identity, conectores de lojas, clientes HTTP, SMTP, LLM providers, jobs.
- `Shared`: tipos comuns, resultados, paginacao, constantes e contratos simples.

Higiene de ficheiros do projeto:

- O repositorio deve conter apenas ficheiros necessarios ao funcionamento, manutencao, testes ou documentacao da aplicacao.
- Nao devem ser mantidos ficheiros lixo, temporarios, screenshots soltos, logs locais, outputs de publish, outputs de build ou ficheiros gerados por ferramentas.
- Artefactos de validacao visual devem ser guardados fora do repositorio ou em pastas ignoradas pelo Git.
- Fixtures de testes so devem existir quando forem usadas por testes automatizados ou validacoes documentadas.
- Datasets grandes ou temporarios devem ficar fora da raiz do projeto; se forem necessarios, devem ir para uma pasta de fixtures com nome claro e referencia nos testes.
- Se uma tarefa gerar ficheiros temporarios, estes devem ser removidos antes de terminar a tarefa.
- Novos ficheiros so devem ser adicionados quando tiverem responsabilidade clara e forem referenciados por codigo, testes, configuracao ou documentacao.

Servicos principais:

- `ProductIntentService`: extrai categoria, restricoes, orcamento, mercado e idioma.
- `ImageProductDiscoveryService`: identifica produto ou categoria a partir de imagem.
- `ProductDiscoveryService`: encontra candidatos de produto.
- `ProductMatcher`: valida se uma oferta corresponde ao produto pretendido.
- `OfferDiscoveryService`: procura ofertas por mercado.
- `StoreConnector`: contrato por loja/marketplace.
- `OfferValidator`: valida URL, preco, vendedor, moeda, stock e correspondencia do produto.
- `OfferRankingService`: ordena produtos e lojas.
- `MarketResolver`: decide mercado, moeda, lojas e regras locais.
- `LlmProviderRouter`: escolhe modelos locais/cloud e compara respostas.
- `PriceAlertService`: cria, atualiza e consulta alertas.
- `PriceAlertMonitor`: job recorrente que valida precos e dispara notificacoes.
- `EmailNotificationService`: envia emails HTML sem assinaturas automaticas indevidas.

## 6. Estrategia de lojas e marketplaces

A aplicacao deve ter conectores por mercado e deve ser preparada para suportar todos os paises desenvolvidos onde existam fontes de dados fiaveis, lojas online relevantes e regras comerciais claras.

Portugal e EUA devem ser tratados como mercados iniciais/exemplos. A arquitetura nao deve assumir que so existem estes dois paises.

Cada mercado deve ter configuracao propria:

- Codigo do pais.
- Culturas/idiomas suportados.
- Moeda.
- IVA/impostos aplicaveis.
- Regras de garantia e devolucao.
- Lojas oficiais relevantes.
- Marketplaces relevantes.
- Comparadores de preco locais.
- Prioridade de conectores.
- Politica de fallback quando nao existir oferta validada.

Portugal deve considerar, entre outras fontes:

- KuantoKusta
- Worten
- Worten Marketplace
- FNAC
- FNAC Marketplace
- Radio Popular
- PCDIGA
- MediaMarkt
- Amazon.es
- Lojas oficiais das marcas
- Outras lojas portuguesas relevantes por categoria

EUA deve considerar fontes proprias desse mercado, por exemplo:

- Amazon.com
- Best Buy
- Walmart
- Target
- B&H
- Newegg
- Lojas oficiais das marcas

Mercados desenvolvidos a suportar progressivamente:

- Uniao Europeia e Espaco Economico Europeu, por exemplo Espanha, Franca, Alemanha, Italia, Paises Baixos, Belgica, Irlanda, Austria, Dinamarca, Suecia, Finlandia e Noruega.
- Reino Unido.
- Suica.
- Canada.
- Australia.
- Nova Zelandia.
- Japao.
- Coreia do Sul.
- Singapura.
- Outros mercados de elevado rendimento quando existirem fontes fiaveis e conectores legais.

Cada novo mercado deve ser adicionado por configuracao e conectores proprios, nao por condicoes hardcoded espalhadas pela UI.

Regras:

- Marketplace e loja nao sao a mesma coisa. A UI deve distinguir "vendido por X via Worten Marketplace", "vendido por X via FNAC Marketplace" ou equivalente.
- Lojas sem preco confirmado nao devem entrar como recomendacao de compra com preco.
- Lojas sem URL direto do produto nao devem ser marcadas como opcao valida.
- Pesquisa generica de loja so pode ser fallback e deve ser identificada como fallback.
- Preferir APIs oficiais, feeds de produto, affiliate feeds ou integracoes permitidas.
- Scraping so deve existir quando permitido pelos termos da fonte, com rate limit, cache e respeito por robots/politicas aplicaveis.

Cada conector deve declarar capacidades:

| Capacidade | Descricao |
| --- | --- |
| `SupportsProductSearch` | Consegue procurar produtos por texto. |
| `SupportsDirectProductUrl` | Consegue devolver URL direto do produto. |
| `SupportsPriceValidation` | Consegue confirmar preco atual. |
| `SupportsStockValidation` | Consegue confirmar disponibilidade. |
| `SupportsSellerExtraction` | Consegue identificar vendedor real. |
| `SupportsMarketplaceOffers` | Consegue distinguir marketplace e vendedor. |
| `SupportsCountryFiltering` | Consegue limitar ao mercado do utilizador. |
| `RequiresManualConfirmation` | So consegue abrir a loja para confirmacao manual. |

Uma loja sem `SupportsDirectProductUrl` e `SupportsPriceValidation` nao pode ser usada como loja recomendada principal.

## 7. Validacao de ofertas

Uma oferta valida deve ter:

- Nome da loja.
- Vendedor real, quando aplicavel.
- URL direto da pagina do produto.
- Preco confirmado.
- Moeda.
- Pais/mercado.
- Timestamp da validacao.
- Estado de stock ou indicacao clara de indisponibilidade.
- Correspondencia suficiente entre produto pesquisado e produto encontrado.

Uma oferta deve ser rejeitada quando:

- A URL aponta para pesquisa, categoria ou homepage.
- O produto pertence a outra categoria.
- O preco ultrapassa um limite explicito do utilizador.
- O preco nao existe ou nao foi confirmado.
- A moeda ou mercado nao correspondem ao utilizador.
- O vendedor e desconhecido num marketplace.
- A pagina encontrada e ambigua.
- A pagina retorna 404, bloqueio, erro ou produto diferente.

Sistema de confianca:

- `ProductMatchScore`: mede se a oferta corresponde ao produto certo.
- `PriceConfidenceScore`: mede se o preco foi realmente confirmado.
- `SellerConfidenceScore`: mede se o vendedor foi identificado.
- `StoreTrustScore`: mede confianca historica da loja/vendedor.
- `UrlConfidenceScore`: mede se a URL e pagina direta de produto.
- `OfferConfidenceScore`: composicao dos scores anteriores.

Regras minimas:

- Abaixo de 70/100, a oferta deve ser rejeitada.
- Entre 70/100 e 84/100, a oferta pode aparecer como "confirmar antes de comprar".
- A partir de 85/100, a oferta pode entrar no ranking principal.
- Uma oferta sem preco confirmado nunca pode ser apresentada como preco final.
- Uma oferta sem URL direto nunca pode ser chamada de recomendacao valida.

Frescura do preco:

- Preco validado ha menos de 30 minutos: pode ser mostrado como confirmado.
- Preco validado entre 30 minutos e 24 horas: pode ser mostrado com aviso de revalidacao recomendada.
- Preco validado ha mais de 24 horas: deve ser revalidado antes de ser recomendado.
- Preco nunca validado: nao entra como preco de compra.

## 8. Ranking de produtos e lojas

O ranking deve combinar:

- Adequacao ao pedido.
- Preco final confirmado.
- Confianca da loja/vendedor.
- Garantia.
- Prazo de entrega.
- Stock.
- Politica de devolucao.
- Historico de preco.
- Risco de marketplace.
- Compatibilidade com restricoes do utilizador.
- Qualidade das fontes usadas.

Formula base do score final:

- 35% adequacao ao pedido.
- 25% preco.
- 15% confianca da loja/vendedor.
- 10% garantia.
- 10% entrega e stock.
- 5% historico de preco.

Regras da formula:

- O score final deve ser calculado de 0 a 100.
- Uma oferta sem preco confirmado ou sem URL direta de produto nao pode vencer o ranking de compra.
- Se o utilizador definir restricoes obrigatorias, como preco maximo, tamanho, compatibilidade ou pais, produtos fora dessas restricoes devem ser excluidos ou claramente marcados como nao elegiveis.
- A adequacao ao pedido vale mais do que preco para evitar recomendar produtos baratos mas errados.
- O preco so deve contar quando existir preco confirmado ou fonte suficientemente confiavel para o tipo de recomendacao.
- Em empate, preferir a oferta com maior confianca; depois menor preco; depois melhor entrega; depois melhor garantia.
- Os pesos podem ser ajustados por categoria, mas qualquer alteracao deve ficar documentada e testada em `docs/TESTING_AND_QUALITY.md`.

O ranking deve ser explicavel. Cada recomendacao deve ter motivos curtos e concretos, por exemplo:

- "Dentro do orcamento"
- "Melhor preco confirmado"
- "Vendedor autorizado"
- "Entrega mais rapida"
- "Melhor garantia"
- "Boa correspondencia com os criterios"

Regra especifica para marketplaces:

- A plataforma e o vendedor real devem ser separados.
- Exemplo correto: "Vendido por Loja X via Worten Marketplace".
- Se o vendedor real nao for identificado, a oferta perde confianca.
- Se o vendedor for internacional num mercado local, isso deve ser indicado.
- Se a garantia, IVA/NIF ou devolucao forem incertos, a oferta deve ser penalizada.

## 9. LLM e validacao cruzada

O LLM deve ser usado para:

- Interpretar perguntas livres.
- Extrair restricoes.
- Classificar categoria.
- Explicar diferencas.
- Resumir reviews e especificacoes.
- Comparar respostas de varios modelos.

O LLM nao deve ser a fonte final para:

- Precos.
- Stock.
- URLs de produto.
- Existencia de uma oferta.
- Garantia real.
- Vendedor real.

Prioridade de modelos:

1. Ollama local, quando disponivel.
2. Modelos cloud do Ollama, escolhidos conforme a tarefa.
3. Fallbacks gratuitos ou outros providers apenas quando o local/cloud configurado falhar.

Quando existirem varios modelos disponiveis, a aplicacao pode chamar mais do que um em paralelo para interpretar ou validar a resposta. O resultado final deve ser combinado e depois confirmado contra dados factuais antes de ser mostrado ao utilizador.

## 10. Pesquisa por imagem

A pesquisa por imagem deve funcionar por:

- Botao de adicionar imagem no campo de conversa.
- Colar imagem com Ctrl+V.
- Upload em mobile.

O fluxo deve:

- Identificar produto exato quando possivel.
- Identificar categoria quando o produto exato nao for confiavel.
- Mostrar confianca da identificacao quando necessario.
- Pedir clarificacao quando a imagem for ambigua.
- Usar o mesmo pipeline de produto, oferta, ranking e validacao da pesquisa por texto.

## 11. Alertas de preco

Um alerta deve guardar:

- Utilizador.
- Produto normalizado.
- Mercado.
- Moeda.
- Preco alvo.
- Lojas ou vendedores preferenciais, se definidos.
- Melhor oferta conhecida no momento da criacao.
- Estado do alerta.
- Ultima verificacao.
- Ultimo email enviado.

O monitor recorrente deve:

- Revalidar ofertas com conectores.
- Guardar snapshots.
- Comparar preco confirmado com preco alvo.
- Enviar email apenas quando o preco confirmado for igual ou inferior ao alvo.
- Evitar emails duplicados para o mesmo evento.
- Registar erros de validacao sem interromper todos os alertas.

O email deve:

- Ter assunto com o nome do produto.
- Ter corpo HTML consistente com a identidade visual da aplicacao.
- Mostrar produto, preco alvo, preco encontrado, loja, vendedor e link direto.
- Avisar para confirmar preco, stock e vendedor na loja antes de pagar.
- Nao incluir assinatura corporativa automatica indesejada.

## 12. Mercado, idioma e moeda

O mercado deve ser resolvido por prioridade:

1. Preferencia explicita do utilizador.
2. Localizacao autorizada pelo utilizador.
3. Idioma/cultura do browser.
4. Fallback configurado.

Regras:

- `pt-PT` deve usar Portugal e EUR por defeito.
- `en-US` deve usar EUA e USD por defeito.
- `en-GB` deve usar Reino Unido e GBP por defeito.
- Culturas da zona euro devem usar EUR e o pais correspondente quando suportado.
- Culturas de mercados desenvolvidos devem mapear para o pais, moeda, idioma e lojas locais correspondentes.
- Texto, datas, numeros e moeda devem seguir a cultura ativa.
- Lojas sugeridas devem pertencer ao mercado ativo.
- Se uma loja estrangeira for sugerida, deve ser claro que e internacional.
- Se o mercado do browser ainda nao tiver conectores suficientes, a aplicacao deve avisar e oferecer mercados alternativos suportados.

Modelo minimo de configuracao de mercado:

| Campo | Regra |
| --- | --- |
| `CountryCode` | Codigo ISO do pais. |
| `DefaultCulture` | Cultura principal do mercado. |
| `SupportedCultures` | Culturas aceites para esse mercado. |
| `CurrencyCode` | Moeda usada nos precos. |
| `VatMode` | Regras de imposto/IVA aplicaveis. |
| `WarrantyRules` | Regras gerais de garantia/devolucao. |
| `PrimaryStores` | Lojas principais. |
| `MarketplaceStores` | Marketplaces e plataformas com vendedores terceiros. |
| `PriceComparisonSources` | Comparadores locais. |
| `ConnectorPriority` | Ordem de tentativa dos conectores. |
| `FallbackMarkets` | Mercados alternativos quando nao houver cobertura suficiente. |

## 13. Historico e continuidade

O historico deve guardar pesquisas reais feitas pelo utilizador, incluindo:

- Query original.
- Produto recomendado.
- Categoria.
- Mercado.
- Data.
- Link para reabrir resultados.

O historico deve ser util para continuar uma comparacao, nao apenas uma lista passiva.

## 14. Metricas de qualidade dos dados

A aplicacao deve medir:

- Percentagem de pesquisas com pelo menos uma oferta validada.
- Percentagem de resultados rejeitados e motivo.
- Tempo medio de validacao.
- Frescura das ofertas.
- Precisao do match produto-oferta.
- Percentagem de cliques que abriram URL direto de produto.
- Falhas por loja/conector.
- Alertas enviados corretamente.

Estas metricas devem guiar melhorias dos conectores e do ranking.

## 14.1 Backoffice tecnico de qualidade

A aplicacao deve ter uma area interna para diagnostico e qualidade dos dados.

O backoffice deve mostrar:

- Pesquisas recentes.
- Produtos candidatos aceites e rejeitados.
- Ofertas aceites e rejeitadas.
- Motivo de rejeicao de cada oferta.
- Ultima validacao por conector.
- Erros por loja/conector.
- URLs abertas pelos utilizadores.
- Precos historicos por produto.
- Alertas ativos e ultimo estado.
- Emails enviados e falhados.
- Respostas dos LLMs quando usadas para interpretacao.
- Diferencas entre modelos quando existir validacao cruzada.

## 14.2 Dataset fixo de regressao

Deve existir um conjunto fixo de pesquisas para regressao automatica e manual. Cada pesquisa deve ter categoria esperada, restricoes obrigatorias, pais, lingua e criterios de validacao.

Casos minimos:

- `carregador de iPhone`: deve devolver carregadores/cabos/MagSafe, nao iPhones.
- `disco externo ate 20 euros`: deve respeitar preco maximo.
- `rato ate 50 euros`: deve devolver ratos, nao teclados ou computadores.
- `tablet robusto 10 polegadas para fabrica`: deve devolver tablets rugged ou industriais.
- `frigorifico Bosch Serie 6`: deve devolver frigorificos da Bosch.
- `MacBook Air M3 14 polegadas`: deve tratar corretamente a inexistencia de MacBook Air 14 se aplicavel.
- `manteiga de amendoim natural`: deve devolver produtos alimentares, nao eletronica.
- `monitor 27 polegadas 144hz`: deve devolver monitores compativeis.
- `aspirador sem fios ate 200 euros`: deve respeitar categoria e orcamento.
- `iPhone 17`: deve devolver modelos reais/disponiveis ou explicar indisponibilidade por mercado.

Cada caso deve validar:

- Categoria correta.
- Respeito por orcamento.
- Pelo menos uma oferta validada quando existir no mercado.
- URL direto de produto para lojas recomendadas.
- Preco visivel e coerente com a loja.
- Lingua e moeda corretas.
- Ausencia de resultados claramente fora da intencao.

## 15. Roadmap tecnico recomendado

MVP operativo minimo:

- Pesquisa por texto com categoria correta.
- Respeito obrigatorio por restricoes explicitas.
- Pelo menos um mercado inicial com 3 ou mais conectores que devolvam URL direto e preco confirmado.
- Estrutura preparada para adicionar todos os mercados desenvolvidos por configuracao.
- Checkout baseado em oferta validada.
- Botao "confirmar preco na loja" a abrir novo separador na pagina real do produto.
- Alertas de preco guardados e monitorizados por job recorrente.
- Email HTML de alerta com produto, preco, loja, vendedor e link direto.
- Historico de pesquisas funcional.
- Dataset de regressao automatizado com Playwright.

### Fase 1 - Fundacao de dados

- Criar modelo de dominio de produtos, lojas, vendedores, ofertas e snapshots.
- Separar preco estimado de preco confirmado.
- Implementar validadores de URL, preco, mercado e correspondencia de produto.
- Garantir que resultados sem validacao ficam marcados como incompletos.

### Fase 2 - Conectores do mercado inicial

- Implementar conectores para as principais fontes do mercado inicial.
- Adicionar suporte a marketplaces e vendedor real.
- Guardar snapshots das ofertas.
- Mostrar apenas lojas com URL direto e preco confirmado como recomendacao valida.

### Fase 3 - Ranking e checkout

- Reescrever ranking para usar ofertas validadas.
- Fazer o checkout receber oferta validada, nao recalcular dados ficticios.
- Abrir loja em novo separador diretamente na pagina do produto.
- Garantir que o preco mostrado bate certo com o preco validado.

### Fase 4 - Alertas reais

- Criar job recorrente de monitorizacao.
- Revalidar precos.
- Enviar emails HTML.
- Guardar historico de envios.
- Evitar duplicados.

### Fase 5 - Multi-mercado

- Adicionar resolucao robusta de mercado.
- Criar conectores por pais e por categoria quando necessario.
- Validar localizacao, idioma, moeda e lojas por mercado.
- Adicionar mercados desenvolvidos por ondas, priorizando cobertura de dados, fiabilidade e legalidade das fontes.

Ondas recomendadas:

- Onda 1: Portugal, Espanha, Franca, Alemanha, Reino Unido e EUA.
- Onda 2: Italia, Paises Baixos, Belgica, Irlanda, Austria, Suica e Canada.
- Onda 3: Australia, Nova Zelandia, Japao, Coreia do Sul, Singapura e paises nordicos.
- Onda 4: outros mercados desenvolvidos com fontes confiaveis.

### Fase 6 - Qualidade e escala

- Criar testes Playwright completos.
- Criar suite de regressao com queries por categoria.
- Monitorizar conectores.
- Adicionar caching, rate limits e observabilidade.

## 16. Decisoes abertas

- Quais fontes terao API/feed oficial.
- Quais lojas permitem scraping ou integracao automatica.
- Qual sera o mercado inicial de producao.
- Qual ordem de expansao para os restantes mercados desenvolvidos.
- Que nivel de geolocalizacao sera pedido ao utilizador.
- Como tratar afiliacao e transparencia comercial.
- Politica de retencao de historico e snapshots.
- Frequencia dos jobs de alerta por plano.
- Limites por plano e custos de chamadas LLM.

## 17. Criterios de entrada em producao / Release gates

A aplicacao so deve ser considerada pronta para producao quando cumprir todos os criterios abaixo.

Qualidade de pesquisa:

- A matriz minima de pesquisas definida em `docs/TESTING_AND_QUALITY.md` passa sem resultados incoerentes.
- Todas as pesquisas com limite de preco respeitam o limite indicado pelo utilizador.
- Pesquisas de acessorios nao devolvem o produto principal como recomendacao.
- Pesquisas sem oferta valida mostram estado claro em vez de inventar resultados.

Qualidade de ofertas e lojas:

- Zero lojas recomendadas sem preco confirmado.
- Zero URLs de pesquisa, categoria ou homepage usadas como URL de produto.
- Toda loja recomendada tem URL direta da pagina do produto.
- Toda oferta de marketplace identifica vendedor real quando aplicavel.
- O preco apresentado na aplicacao bate certo com o preco validado na fonte quando a aplicacao mostra preco confirmado.
- Lojas recomendadas pertencem ao mercado correto do utilizador.

Fluxos criticos:

- Pesquisa por texto funciona de ponta a ponta.
- Pesquisa por imagem funciona por upload e paste quando suportado.
- Detalhe do produto mostra dados coerentes.
- Checkout abre a loja em nova tab na pagina real do produto.
- Alertas de preco sao criados, monitorizados e enviados apenas quando o preco alvo e atingido.
- Emails de alerta renderizam como HTML, incluem o produto no subject e nao incluem assinatura externa indesejada.
- Historico guarda e reabre pesquisas por utilizador.
- Autenticacao, logout, definicoes, creditos e planos funcionam nos fluxos principais.

Qualidade tecnica:

- Testes Playwright criticos passam.
- Testes .NET relevantes passam.
- Testes live externos criticos passam ou ficam explicitamente marcados como skipped por indisponibilidade externa.
- Nao existe scroll horizontal indevido em desktop, tablet ou mobile.
- Nao existem segredos, passwords, tokens reais ou connection strings de producao no repositorio.
- Nao existem ficheiros lixo, screenshots soltos, logs locais, outputs de build/publish ou datasets temporarios no repositorio.
- Logs nao expoem passwords, tokens, API keys ou dados pessoais desnecessarios.
- Erros de producao nao mostram stack traces ao utilizador.

Sem estes criterios cumpridos, a aplicacao pode ser considerada em desenvolvimento ou pre-release, mas nao pronta para producao.

## 18. Regra de manutencao

Atualizar este blueprint sempre que mudar:

- Visao do produto.
- Fluxo principal.
- Modelo de dados.
- Regras de validacao.
- Contrato tecnico de oferta valida.
- Estados da pesquisa.
- Estrategia de lojas/conectores.
- Regras de ranking.
- Alertas.
- LLM providers.
- Mercados suportados.
- Dataset de regressao.
- Backoffice tecnico de qualidade.
- Release gates.

Atualizar `docs/TESTING_AND_QUALITY.md` sempre que uma funcionalidade nova for criada ou uma regra existente mudar.

Sempre que este blueprint for alterado, o `docs/TESTING_AND_QUALITY.md` deve ser revisto no mesmo trabalho. Se a alteracao do blueprint afetar comportamento, arquitetura, qualidade de dados, plataformas suportadas, integracoes, regras de validacao ou criterios de release, o ficheiro de testes tambem deve ser atualizado. Se nao houver impacto em testes, isso deve ficar indicado na resposta final.
