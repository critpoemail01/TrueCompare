# TrueCompare - Testing and Quality

Ultima atualizacao: 2026-05-22

Este ficheiro e a referencia viva para garantir que a TrueCompare continua funcional, coerente e pronta para evoluir. Sempre que uma funcionalidade for criada, alterada ou removida, este documento deve ser atualizado no mesmo trabalho.

O objetivo nao e apenas confirmar que a aplicacao abre. O objetivo e garantir que o utilizador nunca recebe uma recomendacao enganosa de produto, preco, loja, idioma, mercado ou link externo.

## Regra de manutencao

Sempre que for lancada uma funcionalidade:

- adicionar ou atualizar a funcionalidade neste ficheiro;
- definir o comportamento esperado;
- definir criterios de aceite;
- definir testes Playwright E2E quando houver interacao de utilizador;
- definir testes .NET quando houver regra de negocio, dados, LLM, preco, loja, autenticacao, autorizacao ou persistencia;
- indicar riscos residuais se a funcionalidade depender de dados externos ou sites de terceiros.

Sempre que `docs/TRUECOMPARE_BLUEPRINT.md` for alterado:

- rever este ficheiro no mesmo trabalho;
- atualizar criterios de aceite quando o blueprint alterar comportamento esperado;
- atualizar testes Playwright quando o blueprint alterar fluxos de utilizador, UI, plataformas, idiomas, mercados, lojas ou integracoes externas;
- atualizar testes .NET quando o blueprint alterar regras de negocio, validacao, ranking, dados, seguranca, alertas, LLM ou persistencia;
- atualizar a matriz de pesquisas quando o blueprint alterar categorias, mercados, lojas ou regras de recomendacao;
- se a alteracao do blueprint nao exigir alteracao neste ficheiro, indicar essa razao na resposta final.

## Criterio global de pronto

A aplicacao so deve ser considerada pronta quando:

- os fluxos principais funcionam de ponta a ponta;
- os produtos devolvidos fazem sentido para a pesquisa;
- os limites de preco sao respeitados;
- as lojas recomendadas pertencem ao mercado correto;
- uma loja so aparece como opcao valida quando existe preco confirmado e URL direta de produto;
- o botao externo abre uma pagina real do produto, numa nova tab;
- o idioma segue o idioma do browser;
- nao existe scroll horizontal indevido;
- os estados de erro, vazio e loading sao claros;
- os testes Playwright criticos passam;
- os testes .NET relevantes passam;
- nao ha segredos, passwords ou tokens reais no repositorio;
- nao ha regressao critica de seguranca, layout, autenticacao, alertas, historico, creditos, LLM ou checkout.

## Release gates para producao

Estes criterios sao os gates formais de entrada em producao e devem ser revistos sempre que o blueprint mudar.

A aplicacao so pode ser considerada pronta para producao quando todos estes gates passarem.

Pesquisa e recomendacao:

- matriz minima de pesquisas passa;
- pesquisas com limite de preco respeitam o limite;
- acessorios nao devolvem produto principal;
- resultados pertencem a categoria correta;
- quando nao ha oferta valida, existe estado claro;
- nao existem recomendacoes baseadas em dados inventados.

Ofertas e lojas:

- zero lojas recomendadas sem preco confirmado;
- zero URLs de pesquisa/categoria/homepage usadas como URL de produto;
- toda loja recomendada tem URL direta da pagina do produto;
- ofertas marketplace identificam vendedor real quando aplicavel;
- preco mostrado bate certo com a fonte quando a app apresenta preco confirmado;
- resultados de compra nao sugerem produtos sem pelo menos uma oferta validada;
- lojas pertencem ao mercado correto.

Fluxos criticos:

- pesquisa por texto funciona ponta a ponta;
- pesquisa por imagem funciona por upload e paste quando suportado;
- detalhe do produto mostra dados coerentes;
- checkout abre loja em nova tab na pagina real do produto;
- alertas sao criados, monitorizados e enviados apenas quando o preco alvo e atingido;
- emails de alerta renderizam HTML, incluem produto no subject e nao incluem assinatura externa indesejada;
- historico guarda e reabre pesquisas por utilizador;
- autenticacao, logout, definicoes, creditos e planos funcionam.

Qualidade tecnica:

- testes Playwright criticos passam;
- testes .NET relevantes passam;
- testes live externos criticos passam ou ficam skipped com motivo externo claro;
- sem scroll horizontal indevido em desktop, tablet ou mobile;
- sem segredos reais no repositorio;
- sem ficheiros lixo, screenshots soltos, logs locais, outputs de build/publish ou datasets temporarios;
- logs nao expoem passwords, tokens, API keys ou dados pessoais desnecessarios;
- erros de producao nao mostram stack traces ao utilizador.

## Tipos de teste obrigatorios

### Testes Playwright E2E

Usar Playwright para validar comportamento real no browser:

- navegacao entre paginas;
- cliques;
- formularios;
- Enter no campo principal;
- upload e paste de imagem;
- nova tab para loja externa;
- layout responsivo;
- ausencia de scroll horizontal;
- idioma via `Accept-Language`;
- mercado/lojas por locale;
- fluxos autenticados.

### Testes .NET

Usar testes unitarios, integracao ou componentes para validar:

- classificacao da pesquisa;
- selecao de catalogo;
- filtros de preco;
- selecao de lojas por mercado;
- rejeicao de ofertas sem preco ou sem URL direta;
- fallback LLM;
- quotas/creditos;
- alertas;
- email template;
- autenticacao e autorizacao;
- persistencia e migrations quando aplicavel.

### Testes live externos

Separar os testes que dependem de internet e sites de terceiros:

- lojas externas;
- YouTube;
- paginas oficiais de marcas;
- comparadores;
- validacao de preco em tempo real.
- lojas PT/EU adicionais como Darty, Globaldata, Castro Electronica e Aquario,etc, quando houver pagina direta e preco confirmavel.

Estes testes devem ter timeout, retry limitado e possibilidade de skip quando a rede ou o site externo falhar. Nao devem bloquear a suite deterministica.

## Funcionalidades e validacoes

## 1. Layout, navegacao e shell da aplicacao

Paginas envolvidas:

- `/`
- `/results`
- `/product/{slug}`
- `/checkout`
- `/alerts`
- `/history`
- `/credits`
- `/settings`
- `/login`
- `/register`

Validar:

- sidebar aparece corretamente;
- topbar mostra Login/Criar conta quando anonimo;
- topbar mostra plano, logout e avatar quando autenticado;
- menu tem Novo Chat, Alertas ativos, Historico e Definicoes;
- Definicoes fica no fundo do menu;
- footer mostra "TrueCompare by Advance" com link correto para `https://www.advance.com.pt/`;
- links internos nao quebram;
- pagina inexistente mostra erro amigavel;
- nao ha scroll horizontal;
- layout funciona em desktop, tablet e mobile.

Playwright:

- `layout_anonymous_navigation.spec`
- `layout_authenticated_navigation.spec`
- `layout_no_horizontal_scroll.spec`
- `layout_responsive_sidebar.spec`

## 2. Pagina inicial e pesquisa por texto

Pagina:

- `/`

Validar:

- campo principal esta centrado e com placeholder correto;
- campo nao vem preenchido por defeito;
- texto placeholder desaparece ao escrever;
- clicar no campo permite escrever;
- carregar Enter inicia o fluxo;
- clicar no botao inicia o fluxo;
- query validavel e enviada para `/results`;
- se o utilizador anonimo precisar de login, o `returnUrl` aponta para `/results?query=...`;
- categorias sugeridas nao ficam selecionadas ao carregar a pagina;
- colar texto normal funciona;
- estado sem creditos ou nao autenticado e tratado corretamente;
- sem scroll vertical desnecessario no primeiro ecra quando aplicavel.

Casos obrigatorios:

- `iPhone 17`
- `carregador de iPhone`
- `rato ate 50 euros`
- `disco externo ate 20 euros`
- `tablet resistente para chao de fabrica`
- `MacBook Air M3`
- `frigorifico Bosch Serie 6`

Playwright:

- `home_search_with_enter.spec`
- `home_search_with_button.spec`
- `home_placeholder_and_empty_state.spec`
- `home_category_not_preselected.spec`
- `home_no_unwanted_scroll.spec`

## 3. Pesquisa por imagem

Pagina:

- `/`

Validar upload pelo botao `+`:

- clicar no `+` abre seletor de ficheiros;
- aceita `.png`, `.jpg`, `.jpeg`, `.webp`;
- mostra preview da imagem;
- permite pesquisar com a imagem;
- permite remover ou substituir a imagem;
- ficheiro invalido mostra erro claro;
- ficheiro grande nao quebra layout;
- imagem pequena ou pouco clara tem fallback adequado;
- imagem sem produto claro nao deve gerar produto inventado.

Validar colagem com `Ctrl+V`:

- copiar imagem para clipboard;
- focar area de pesquisa;
- usar `Ctrl+V`;
- preview aparece;
- pesquisa usa imagem;
- colar texto normal continua a funcionar;
- colar texto e imagem nao quebra o input;
- em mobile o upload continua utilizavel.

Validar LLM/visao:

- se houver modelo de visao, resultado deve ser coerente com a imagem;
- se o modelo falhar, mostrar fallback compreensivel;
- se nao houver modelo de visao, a app deve explicar que a imagem nao foi identificada e continuar com pesquisa por texto;
- a imagem nao deve ser guardada desnecessariamente sem autorizacao.

Playwright:

- `home_image_upload_plus_button.spec`
- `home_image_paste_ctrl_v.spec`
- `home_paste_text_does_not_trigger_image.spec`
- `home_invalid_image_file.spec`
- `home_image_no_vision_model_fallback.spec`

## 4. Ranking automatico e restricoes de pesquisa

Paginas e servicos:

- `/results`
- servicos de descoberta, ranking e validacao de ofertas

Validar:

- query aparece claramente;
- o utilizador nao passa por ecra manual de ponderacao;
- a rota antiga `/criteria` redireciona para `/results`;
- os pesos internos mudam por categoria quando aplicavel;
- formula base do ranking respeita os pesos do blueprint: 35% adequacao, 25% preco, 15% confianca, 10% garantia, 10% entrega/stock e 5% historico de preco;
- produtos fora de restricoes obrigatorias, como preco maximo ou categoria, nao vencem o ranking;
- ofertas sem preco confirmado ou sem URL direta de produto nao vencem o ranking de compra;
- desempates seguem a ordem: confianca, menor preco, entrega e garantia;
- estado sem query e tratado;
- o resultado fica acessivel por teclado e sem scroll horizontal.

Exemplos:

- rato: sensor, ergonomia, bateria, preco;
- telemovel: camara, bateria, suporte, ecra;
- portatil: CPU, autonomia, peso, ecra;
- tablet industrial: resistencia, brilho, bateria, certificacoes.

Playwright:

- `results_direct_search_flow.spec`
- `results_category_specific_ranking.spec`
- `results_keyboard_access.spec`

.NET:

- `RankingScoreCalculator_UsesBlueprintWeights`
- `RankingScoreCalculator_ExcludesProductsOutsideMandatoryConstraints`
- `RankingScoreCalculator_DoesNotPromoteOffersWithoutConfirmedPriceOrProductUrl`
- `RankingScoreCalculator_TieBreaksByTrustThenPriceThenDeliveryThenWarranty`

## 5. Resultados comparativos

Pagina:

- `/results`

Validar:

- produtos pertencem a categoria correta;
- produto recomendado corresponde a intencao;
- nao aparecem produtos irrelevantes;
- nao ha duplicacao entre sugestao IA e cards;
- apenas uma sugestao principal;
- cards mostram nome, preco, score, specs e badge;
- o cabecalho nao mostra botoes redundantes como `Continuar com recomendado` ou `Adicionar produto`;
- botao `Ver detalhes` abre o produto correto;
- sem caracteres corrompidos;
- sem scroll horizontal;
- cards adaptam em mobile.

Casos criticos:

- `iPhone 17` devolve familia iPhone 17;
- `carregador de iPhone` devolve carregadores, nao iPhones;
- `rato ate 50 euros` devolve ratos dentro do limite;
- `disco externo ate 20 euros` nao devolve discos acima do limite;
- `tablet resistente para chao de fabrica` devolve tablets rugged/industriais;
- erros ortograficos continuam coerentes;
- query vaga tem resultado util ou estado claro.

Playwright:

- `results_quality_matrix.spec`
- `results_budget_limits.spec`
- `results_accessory_vs_main_product.spec`
- `results_typo_tolerance.spec`
- `results_cards_navigation.spec`

.NET:

- catalogo correto por query;
- ranking por query;
- filtro de preco maximo;
- zero resultados quando nao ha produto valido dentro do limite;
- rejeicao de categoria errada.

## 6. Detalhe do produto

Pagina:

- `/product/{slug}`

Validar:

- produto corresponde ao card clicado;
- nome, marca, preco, score e specs estao coerentes;
- pagina oficial existe quando apresentada;
- link `Abrir pagina oficial` nao abre 404;
- link oficial e sobre o produto certo;
- reviews do YouTube sao sobre o produto certo;
- reviews abrem numa nova tab;
- nao mostrar lojas nesta pagina se lojas pertencem ao checkout;
- botao `Ver condicoes` leva ao checkout do mesmo produto;
- produto inexistente mostra estado claro;
- query original e preservada quando necessaria.

Playwright:

- `product_detail_correct_product.spec`
- `product_detail_official_page.spec`
- `product_detail_youtube_reviews.spec`
- `product_detail_checkout_next_step.spec`

Live externo:

- validar pagina oficial para Apple, Logitech, Bosch, Getac, Samsung;
- validar que pesquisa YouTube contem nome do produto.

## 7. Checkout e condicoes de compra

Pagina:

- `/checkout`

Regra critica:

Uma loja so pode aparecer como opcao valida se tiver:

- preco confirmado;
- URL direta da pagina de produto;
- vendedor identificado;
- produto correspondente;
- mercado correto;
- estado de confianca definido.

Validar:

- nome do produto aparece no topo;
- tabela mostra apenas lojas validadas;
- nao aparecem URLs de pesquisa generica;
- preco aparece quando confirmado;
- quando nao ha preco confirmado, nao mostrar valor inventado;
- quando nao ha loja validada, mostrar `Sem loja validada`;
- subtotal, IVA e total so usam preco confirmado;
- botao da loja abre nova tab;
- nova tab abre pagina de produto;
- URL externa nao e pagina de pesquisa;
- preco mostrado bate certo com a loja quando validavel;
- selecionar loja atualiza painel lateral;
- `Encomendar agora` e `Alertar preco` sao distinguiveis;
- `Alertar preco` cria alerta corretamente;
- em mobile o checkout continua utilizavel.

Casos obrigatorios:

- produto com oferta validada e URL direta;
- produto sem oferta validada;
- produto com apenas URL de pesquisa deve ser rejeitado;
- loja de outro mercado deve ser rejeitada;
- preco live abaixo/acima do preco alvo.

Playwright:

- `checkout_validated_store_only.spec`
- `checkout_no_search_urls.spec`
- `checkout_external_product_tab.spec`
- `checkout_price_matches_store_live.spec`
- `checkout_no_confirmed_offer_empty_state.spec`
- `checkout_action_tabs.spec`

.NET:

- `SellerOffer` valido exige `IsLivePrice` e URL direta;
- rejeitar `/pesquisa`, `/search`, query params de pesquisa;
- mercado PT nao recebe lojas US;
- mercado US nao recebe lojas PT;
- melhor oferta preferida por preco e confianca.

## 7.1 Cobertura de categorias KuantoKusta e Worten

Validar que cada categoria oficial usada como sugestao ou consulta devolve artigos coerentes e chega a checkout com loja validada.

KuantoKusta:

- Electrodomesticos;
- Saude e Beleza;
- Informatica;
- Smartphones e Acessorios;
- Imagem e Som;
- Gaming;
- Animais de Estimacao;
- Puericultura e Brinquedos;
- Bricolagem e Construcao;
- Casa e Decoracao;
- Desporto;
- Moda e Acessorios;
- Auto e Moto;
- Escritorio e Papelaria;
- Cultura e Lazer;
- Gastronomia e Vinhos.

Worten:

- Recondicionados e Outlet;
- Eletrodomesticos;
- Grandes eletrodomesticos;
- Pequenos eletrodomesticos;
- Maquinas de lavar;
- Frigorificos;
- Ventoinhas;
- Preparacao de alimentos;
- Aspiradores;
- Telemoveis e Smartwatches;
- Informatica;
- Computadores e tablets;
- TV e Som;
- Gaming;
- Jogos e Brinquedos;
- Fotografia, Drones e Video;
- Beleza e Saude;
- Cuidado Pessoal e Saude;
- Perfumaria e Cosmetica;
- Bebe;
- Casa e Decoracao;
- Sofas;
- Jardim;
- Bricolage;
- Bricolage e Jardim;
- Desporto, Outdoor e Viagem;
- Fitness;
- Mobilidade;
- Mobilidade, Auto e Moto;
- Livros, Musica e Filmes;
- Escritorio e Papelaria.

Regras de teste:

- a categoria nao pode devolver artigo fora da familia;
- a lista de resultados nao pode conter produto sem checkout validado;
- `Ver detalhes` e `Ver condicoes` devem manter o mesmo produto;
- checkout deve ter pelo menos uma loja validada ou mostrar claramente `Sem loja validada`;
- lojas validas exigem URL direta, preco confirmado e mercado correto;
- Playwright deve percorrer todas as sugestoes da home;
- testes .NET devem cobrir as categorias oficiais de ambas as fontes.

## 8. Lojas externas e preco

Validar lojas:

- Radio Popular;
- Worten;
- PCDIGA;
- KuantoKusta;
- Amazon.es;
- Amazon.com;
- Apple Store PT/US;
- FNAC;
- MediaMarkt;
- Best Buy;
- Walmart;
- B&H;
- Newegg.

Para cada loja validada:

- link abre;
- nao da 404;
- produto corresponde;
- preco corresponde quando a app apresenta preco confirmado;
- stock e variacao sao tratados com cautela;
- cookies/popups nao impedem validacao essencial.

Live externo:

- suite separada `external_store_validation.live.spec`;
- timeouts e skips para indisponibilidade de terceiros;
- nunca efetuar compra real.

## 9. Mercado, pais e moeda

Validar Portugal:

- locale `pt-PT`;
- UI em portugues;
- lojas PT/EU: Worten, FNAC, PCDIGA, Radio Popular, KuantoKusta, Amazon.es, Apple Store PT;
- moeda EUR;
- formato `1 299,00 EUR` ou formato pt-PT equivalente;
- sem Best Buy, Walmart, Target, Newegg como recomendacao valida.

Validar Estados Unidos:

- locale `en-US`;
- UI em ingles;
- lojas US: Best Buy, Walmart, Amazon.com, Apple US, B&H, Newegg;
- moeda USD quando suportado;
- formato `$1,299.00`;
- sem Worten, PCDIGA, KuantoKusta, Radio Popular como recomendacao valida.

Validar fallback:

- idioma nao suportado cai para `pt-PT`;
- mercado desconhecido nao gera recomendacoes incoerentes;
- loja internacional so aparece se for adequada ao mercado.

Playwright:

- `localization_pt_pt_market.spec`
- `localization_en_us_market.spec`
- `localization_unsupported_fallback.spec`

.NET:

- `ResolveUserMarket`;
- localizacao de produtos/ofertas;
- formatos de moeda;
- textos traduzidos.

## 10. Idioma da aplicacao

Validar:

- `Accept-Language: pt-PT` mostra portugues;
- `Accept-Language: en-US` mostra ingles;
- menus traduzidos;
- botoes traduzidos;
- empty states traduzidos;
- erros traduzidos;
- titulos de paginas traduzidos;
- datas/numeros/moeda por cultura;
- html lang correto;
- response header com content-language quando aplicavel.

Playwright:

- `language_accept_header_pt.spec`
- `language_accept_header_en.spec`

.NET:

- RequestLocalization configurado;
- AppText traduz corretamente;
- fallback para pt-PT.

## 11. LLM, Ollama e fallback

Validar:

- Ollama local e tentado primeiro;
- endpoint local configurado e usado;
- modelos cloud disponiveis podem ser descobertos;
- modelos locais/cloud podem ser chamados em paralelo quando configurado;
- perfil `FAST` e usado para extracao simples, classificacao inicial e normalizacao de texto;
- perfil `BALANCED` e usado para gerar candidatos, comparar especificacoes e explicar diferencas;
- perfil `HIGH_CONFIDENCE` e usado antes de recomendacoes finais, checkout, lojas, precos e alertas;
- queries com preco maximo, compatibilidade critica, uso profissional/industrial, alto valor ou risco elevado sobem automaticamente para `HIGH_CONFIDENCE`;
- divergencia entre modelos em categoria, produto ou elegibilidade bloqueia recomendacao final ate validacao adicional;
- respostas sao validadas antes de ir para UI;
- respostas de categorias erradas sao rejeitadas;
- resultados de varios modelos sao comparados/mesclados quando aplicavel;
- fallback gratuito so e usado se local falhar;
- timeout nao bloqueia UI;
- sem API key, provider e ignorado;
- erro LLM mostra estado claro;
- nao inventar preco, loja ou URL.

Playwright:

- `llm_fallback_user_message.spec`
- `llm_search_result_validation.spec`

.NET:

- prioridade local;
- fallback para providers;
- cooldown/rate limit de provider;
- validacao de JSON;
- merge de respostas paralelas;
- rejeicao de respostas irrelevantes.

## 12. Alertas de preco

Paginas/endpoints:

- `/alerts`
- `/price-alerts/create`
- `TargetPriceMonitorService`
- `PriceAlertEmailTemplate`

Validar:

- criar alerta a partir do checkout;
- preco alvo obrigatorio;
- preco alvo positivo;
- alerta aparece em `Alertas ativos`;
- `Ver condicoes` funciona;
- estado vazio funciona;
- layout esta profissional;
- monitor recorrente avalia alertas ativos;
- email so e enviado quando preco confirmado <= alvo;
- nao enviar email duplicado;
- email contem produto no titulo;
- template HTML renderiza como template, nao texto simples;
- assinatura SMTP externa nao aparece;
- email usa remetente configurado;
- em testes usar fake mail sender.

Playwright:

- `alerts_create_from_checkout.spec`
- `alerts_active_list.spec`
- `alerts_empty_state.spec`
- `alerts_layout.spec`

.NET:

- parse de preco alvo;
- criacao de alerta;
- monitor envia quando deve;
- monitor nao envia duplicado;
- template HTML escapa valores externos;
- subject contem produto.

## 13. Historico

Pagina:

- `/history`

Validar:

- pesquisa fica registada;
- historico e por utilizador;
- estado vazio e claro;
- clicar em item reabre comparacao correta;
- nao mistura historico entre utilizadores;
- logout/login preserva apenas o historico do utilizador;
- pesquisas duplicadas sao tratadas conforme regra definida;
- layout responsivo.

Playwright:

- `history_records_search.spec`
- `history_reopen_search.spec`
- `history_empty_state.spec`
- `history_per_user_isolation.spec`

## 14. Autenticacao

Paginas/endpoints:

- `/login`
- `/register`
- `/auth/login`
- `/auth/register`
- `/auth/logout`
- `/auth/google`

Validar:

- criar conta;
- login;
- logout;
- password errada;
- email invalido;
- password fraca;
- email duplicado;
- remember me;
- redirecionamento de returnUrl;
- paginas protegidas redirecionam para login;
- usuario autenticado ve menu correto;
- usuario anonimo nao acede a alertas, historico, settings.

Playwright:

- `auth_register_login_logout.spec`
- `auth_validation_errors.spec`
- `auth_protected_routes.spec`

.NET:

- Identity;
- password policy;
- unique email;
- external login fallback;
- antiforgery.

## 15. Definicoes do utilizador

Pagina:

- `/settings`

Endpoints:

- `/auth/settings/profile`
- `/auth/settings/password`

Validar:

- so autenticado acede;
- mostra dados atuais;
- altera nome;
- altera email;
- altera password;
- password atual errada bloqueia;
- mensagens de erro/sucesso claras;
- formularios validam campos;
- nao expor dados sensiveis;
- layout responsivo.

Playwright:

- `settings_profile_update.spec`
- `settings_password_update.spec`
- `settings_auth_required.spec`

.NET:

- atualizacao de perfil;
- validacao de email;
- alteracao de password;
- erros Identity.

## 16. Creditos e planos

Paginas:

- `/credits`
- `/credits/success`

Endpoints:

- `/billing/create-checkout-session`
- `/billing/create-subscription-session`
- `/billing/stripe-webhook`

Validar:

- indicador de plano atual;
- creditos restantes;
- modo local ilimitado em localhost;
- pesquisa consome credito quando deve;
- pesquisa nao consome credito em modo local ilimitado;
- sem creditos bloqueia fluxo pago;
- subscricao/plano recorrente mostra informacao correta;
- compra avulsa mostra informacao correta;
- sucesso de creditos funciona;
- layout nao ocupa espaco excessivo.

Playwright:

- `credits_page_layout.spec`
- `credits_local_unlimited.spec`
- `credits_no_credits_blocks_search.spec`

.NET:

- SearchQuotaService;
- consumo de credito;
- modo localhost ilimitado;
- status de subscricao.

## 17. Email

Endpoints de desenvolvimento:

- `/dev/email/price-alert-preview`
- `/dev/email/send-price-alert-test`

Validar:

- SMTP configurado envia HTML real;
- sem SMTP configurado nao rebenta app;
- remetente aparece como TrueCompare quando configurado;
- subject inclui produto;
- body e HTML coerente com a app;
- sem assinatura externa injetada;
- links funcionam;
- caracteres portugueses renderizam bem;
- valores externos sao escapados.

.NET:

- template HTML;
- subject;
- escaping;
- mail sender com mock/fake.

## 18. Seguranca

Validar:

- CSRF nos formularios;
- XSS em query, nome, email, produto;
- Razor encoding ativo;
- sem `MarkupString` para input do utilizador;
- paginas protegidas;
- cookies seguros;
- rate limit login e endpoints sensiveis;
- security headers;
- CORS restritivo se houver API;
- sem segredos reais no repositorio;
- logs sem passwords/tokens/API keys;
- stack trace escondida em producao.

Testes:

- inputs com `<script>`;
- formularios sem antiforgery;
- acesso anonimo a rotas protegidas;
- tentativa de redirect inseguro;
- scan manual de `appsettings` e codigo por segredos.

## 19. Base de dados e persistencia

Validar:

- SQL Server configurado;
- migrations aplicam automaticamente quando configurado;
- nao usar `EnsureCreated`;
- roles seed: Admin, Manager, User;
- admin seed vem de configuracao/secret;
- historico persiste;
- alertas persistem;
- creditos persistem;
- soft delete onde aplicavel;
- audit logs onde aplicavel;
- erros de migracao param startup com log critico.

.NET:

- migrations;
- DbContext;
- seed roles/admin;
- entidades principais;
- indices e constraints.

## 20. Performance e robustez

Validar:

- pesquisa nao bloqueia indefinidamente;
- loading state claro;
- timeout LLM tratado;
- falha de loja externa nao quebra UI;
- falha de email nao quebra checkout;
- pagina inicial leve;
- mobile fluido;
- sem N+1 evidente;
- listas paginadas quando crescerem.

Testes:

- timeouts simulados;
- providers falham;
- base de dados indisponivel;
- rede externa indisponivel;
- muitas pesquisas no historico;
- muitos alertas ativos.

## 21. Responsivo e acessibilidade

Viewports obrigatorios:

- `390x844` mobile;
- `768x1024` tablet;
- `1366x768` laptop;
- `1920x1080` desktop.

Validar:

- sem scroll horizontal;
- textos nao sobrepoem;
- tabelas usaveis;
- botoes com tamanho touch;
- foco visivel;
- navegacao por teclado;
- inputs com label;
- aria-label quando necessario;
- contraste adequado;
- nao depender apenas de cor.

Playwright:

- `responsive_home.spec`
- `responsive_results.spec`
- `responsive_checkout.spec`
- `accessibility_keyboard_navigation.spec`
- `accessibility_focus_visible.spec`

## 22. Matriz minima de pesquisas de qualidade

Esta matriz deve crescer com novos catalogos.

| Pesquisa | Resultado esperado | Validacoes |
| --- | --- | --- |
| iPhone 17 | iPhones/iPhone 17 | sem Androids, lojas do mercado correto |
| carregador de iPhone | carregadores | nao devolver iPhone como produto principal |
| rato ate 50 euros | ratos <= 50 EUR | limite de preco respeitado |
| disco externo ate 20 euros | vazio ou produto real <= 20 EUR | nao sugerir discos de 50 EUR+ |
| tablet resistente para chao de fabrica | tablet rugged | specs industriais |
| MacBook Air M3 | MacBook Air M3 | Apple/ofertas corretas |
| frigorifico Bosch Serie 6 | frigorifico | nao devolver tablet/portatil |
| monitor 27 polegadas 144hz | monitor | specs coerentes |
| teclado mecanico | teclado | nao devolver rato |
| cadeira escritorio ergonomica | cadeira | ergonomia e apoio lombar coerentes |
| maquina de cafe automatica | maquina de cafe | categoria correta |
| pneu 205/55 R16 | pneus | medida respeitada |
| impressora wifi barata | impressora | categoria correta |

## 23. Suite Playwright recomendada

Estrutura sugerida:

```text
tests/e2e/
  home-search.spec.ts
  home-image.spec.ts
  results-quality.spec.ts
  product-detail.spec.ts
  checkout-store-validation.spec.ts
  external-store-validation.live.spec.ts
  alerts.spec.ts
  history.spec.ts
  auth-settings.spec.ts
  credits.spec.ts
  localization-market.spec.ts
  responsive.spec.ts
  accessibility.spec.ts
```

Helpers sugeridos:

- `goHome()`;
- `searchFor(query)`;
- `pasteImage(file)`;
- `uploadImage(file)`;
- `expectNoHorizontalScroll()`;
- `expectLocale(locale)`;
- `expectMarketStores(market)`;
- `openRecommendedProduct()`;
- `openCheckout()`;
- `expectExternalProductTab()`;
- `loginAsTestUser()`;
- `createPriceAlert()`.

## 24. Mobile readiness, PWA e apps nativas

Objetivo:

- garantir que a web continua a funcionar;
- preparar a aplicacao para Android/iOS sem duplicar regras criticas;
- validar que a futura app mobile usa o servidor como fonte de verdade.

Validar web mobile/PWA:

- layout mobile sem scroll horizontal;
- upload por botao funciona em mobile;
- paste de imagem continua suportado onde o browser permitir;
- campos e botoes tem tamanho touch;
- checkout e alertas sao usaveis em ecras pequenos;
- PWA tem manifesto valido quando for ativada;
- icones e cores de PWA existem quando for ativada;
- comportamento offline/de rede fraca mostra estado claro;
- notificacoes push, se existirem, sao opt-in e respeitam permissao do utilizador.

Validar preparacao MAUI Blazor Hybrid:

- UI partilhavel fica isolada em Razor Class Library quando a app mobile for criada;
- componentes partilhados nao dependem diretamente de APIs web-only;
- funcionalidades nativas ficam atras de interfaces como `ICameraService`, `IImagePickerService`, `ILocationService`, `IClipboardService`, `IPushNotificationService`, `ISecureStorageService` e `IExternalBrowserService`;
- implementacoes web e mobile sao registadas por DI;
- app mobile nao acede diretamente a SQL Server;
- app mobile nao guarda LLM keys, SMTP secrets, Stripe secret keys ou credenciais de conectores;
- app mobile comunica com o servidor por APIs autenticadas;
- tokens mobile ficam em secure storage;
- alertas continuam a ser monitorizados pelo backend.

Testes esperados quando mobile/PWA existir:

- Playwright mobile viewport para fluxos web criticos;
- testes de manifesto/service worker para PWA;
- testes de API para endpoints consumidos por mobile;
- testes MAUI em Android emulator;
- testes MAUI em iOS simulator quando houver ambiente macOS;
- testes de permissoes para camera, galeria, localizacao, clipboard e push;
- testes de login/logout e refresh token no mobile;
- testes de abertura de loja externa em browser externo.

## 25. Higiene de ficheiros e repositorio

Validar:

- nao existem `bin/`, `obj/`, `.vs/`, `.codex-run/` ou `_test-run/` versionados;
- nao existem screenshots soltos como `truecompare-*.png`;
- nao existem logs locais versionados;
- nao existem datasets temporarios na raiz do projeto;
- fixtures de teste ficam em pasta clara, por exemplo `TrueCompare.Tests/Fixtures`;
- fixtures grandes so existem quando sao usadas por testes automatizados ou validacoes documentadas;
- ficheiros novos tem responsabilidade clara e sao referenciados por codigo, testes, configuracao ou documentacao;
- outputs de publish nao entram no repositorio;
- `.gitignore` cobre artefactos gerados pelas ferramentas usadas.

Testes/verificacoes:

- `git status --short` antes de finalizar;
- `rg --files` para procurar artefactos inesperados;
- procura por screenshots/logs/datasets temporarios;
- confirmar que documentos renomeados nao deixam referencias antigas.

## 26. Regras para atualizar este documento

Ao criar uma funcionalidade nova:

1. adicionar a funcionalidade na seccao correta;
2. se nao existir seccao, criar uma nova;
3. adicionar criterios de aceite;
4. adicionar testes Playwright esperados;
5. adicionar testes .NET esperados;
6. indicar se ha dependencia externa;
7. atualizar a matriz de pesquisas se a funcionalidade afetar produtos, lojas, idiomas ou mercados.

Ao corrigir um bug:

1. adicionar o bug como caso de regressao;
2. indicar o comportamento esperado;
3. criar teste que falharia antes da correcao;
4. manter o teste na suite.

## 27. Risco residual conhecido

Mesmo com todos os testes, ha risco em:

- precos de lojas externas mudarem;
- lojas alterarem HTML/URLs;
- cookies/popups bloquearem validacao;
- LLM devolver respostas inconsistentes;
- disponibilidade de providers externos;
- modelos Ollama mudarem comportamento;
- sites oficiais alterarem rotas;
- YouTube mudar resultados.

Por isso:

- dados externos devem ser validados em testes live separados;
- recomendacoes de compra so devem aparecer quando preco e URL direta forem confirmados;
- quando nao houver confirmacao, mostrar estado claro em vez de inventar.
