import { expect, test } from '@playwright/test';

test.use({
  locale: 'pt-PT',
  extraHTTPHeaders: { 'Accept-Language': 'pt-PT,pt;q=0.9' }
});

async function registerUser(page) {
  const email = `category-${Date.now()}-${Math.round(Math.random() * 100000)}@example.test`;
  const password = 'CategoryTest1!';

  await page.goto('/register?returnUrl=%2F');
  await page.waitForLoadState('networkidle');
  const emailInput = page.locator('input[name="Email"]');
  await emailInput.fill(email);
  await expect(emailInput).toHaveValue(email);
  await page.locator('input[name="Password"]').fill(password);
  await page.locator('input[name="ConfirmPassword"]').fill(password);
  await page.locator('form.auth-form button[type="submit"]').click();
  await expect(page).toHaveURL(/\/$/);
}

async function expectBlazorErrorHidden(page) {
  await expect(page.locator('#blazor-error-ui')).not.toBeVisible();
}

test('anonymous home search by Enter preserves the query in the login return URL', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);
  const prompt = page.locator('#prompt');

  await expect(prompt).toBeVisible();
  await expect(page.locator('.topbar-market-indicator')).toContainText(/Portugal/);
  await expect(page.locator('.topbar-market-indicator')).toContainText(/pt-PT/);
  await expect(page.locator('.topbar-llm-indicator')).toContainText(/Ollama local/i);
  await expect(page.locator('.topbar-llm-indicator')).toContainText(/qwen3-vl:235b-cloud/i);
  await prompt.fill('rato ate 50 euros');
  await prompt.press('Enter');

  await expect(page).toHaveURL(/\/login\?returnUrl=/);
  expect(decodeURIComponent(page.url())).toContain('/results?query=rato%20ate%2050%20euros');
});

test('home behaves like a chat and asks for more detail before unknown product searches', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);

  await page.locator('#prompt').fill('asdasdasd');
  await page.locator('.search-send').click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.home-chat')).toBeVisible();
  await expect(page.locator('.home-chat')).toContainText(/asdasdasd/);
  await expect(page.locator('.home-chat-message-assistant')).toContainText(/produto|product/i);
  await expect(page).not.toHaveURL(/\/login|\/criteria|\/results/);
  await expectBlazorErrorHidden(page);

  await page.locator('#prompt').fill('asdasdasd');
  await page.locator('.search-send').click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.home-chat-message-user')).toHaveCount(2);
  await expect(page.locator('.home-chat-message-assistant')).toHaveCount(2);
  await expectBlazorErrorHidden(page);

  for (let index = 0; index < 6; index += 1) {
    await page.locator('#prompt').fill(`asdasdasd ${index}`);
    await page.locator('.search-send').click();
  }

  await expect(page.locator('#prompt')).toBeVisible();
  await expect(page.locator('.search-send')).toBeVisible();
  await expect(page.locator('.category-menu')).toBeHidden();
  await expect(page.locator('.home-chat-message-user')).toHaveCount(6);
  await expect(page.locator('.home-chat-message-assistant')).toHaveCount(6);
  await expectBlazorErrorHidden(page);

  const layout = await page.evaluate(() => {
    const prompt = document.querySelector('.prompt-panel.mock-prompt')?.getBoundingClientRect();
    const strip = document.querySelector('.capability-strip')?.getBoundingClientRect();
    const chat = document.querySelector('.home-chat')?.getBoundingClientRect();
    return {
      promptAboveStrip: Boolean(prompt && strip && prompt.bottom <= strip.top - 8),
      chatHasRoom: Boolean(chat && chat.height >= 120)
    };
  });
  expect(layout.promptAboveStrip).toBeTruthy();
  expect(layout.chatHasRoom).toBeTruthy();
});

test('home chat gives fridge options with direct links and asks for more detail', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.locator('#prompt').fill('Frigorificos da me os url para comprar');
  await page.locator('.search-send').click();

  await expect(page).toHaveURL(/\/$/);
  const assistant = page.locator('.home-chat-message-assistant').last();
  await expect(assistant).toContainText(/frigorificos/i);
  await expect(assistant).toContainText(/Links diretos/i);
  await expect(assistant).toContainText(/Melhor escolha preco\/qualidade/i);
  await expect(assistant).toContainText(/familia/i);
  await expect(assistant).toContainText(/Para afinar/i);
  await expect(assistant).toContainText(/https:\/\/www\.worten\.pt\/produtos\/frigorifico/i);
  await expect(page.locator('.home-chat-product')).toHaveCount(4);
  await expect(page.locator('.home-chat-products')).toContainText(/Samsung RB34C600ESA/i);
  await expect(page.locator('.home-chat-products')).toContainText(/Bosch KGN497LDF/i);
  await expect(page.locator('.home-chat-products')).not.toContainText(/PlayStation/i);
  await expect(page).not.toHaveURL(/\/results|\/checkout|\/criteria/);
  await expectBlazorErrorHidden(page);

  const layout = await page.evaluate(() => {
    const chat = document.querySelector('.home-chat')?.getBoundingClientRect();
    const prompt = document.querySelector('.prompt-panel.mock-prompt')?.getBoundingClientRect();
    return {
      chatWideEnough: Boolean(chat && chat.width >= 900),
      promptWideEnough: Boolean(prompt && prompt.width >= 850),
      noHorizontalOverflow: document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1
    };
  });
  expect(layout.chatWideEnough).toBeTruthy();
  expect(layout.promptWideEnough).toBeTruthy();
  expect(layout.noHorizontalOverflow).toBeTruthy();
});

test('home chat gives product advice before purchase validation for industrial tablets', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.locator('#prompt').fill('tablet industrial');
  await page.locator('.search-send').click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.home-chat-message-assistant')).toContainText(/tablet industrial/i);
  await expect(page.locator('.home-chat-message-assistant')).toContainText(/Windows/i);
  await expect(page.locator('.home-chat-message-assistant')).toContainText(/Android/i);
  await expect(page.locator('.home-chat-products')).toBeVisible();
  await expect(page.locator('.home-chat-product')).toHaveCount(4);
  await expect(page.locator('.home-chat-products')).toContainText(/Getac UX10 G3/);
  await expect(page.locator('.home-chat-products')).toContainText(/Zebra ET45/);
  await expect(page.getByRole('button', { name: /Getac UX10 G3/i })).toBeVisible();
  await expect(page).not.toHaveURL(/\/checkout|\/results|\/criteria/);
  await expectBlazorErrorHidden(page);

  await page.getByRole('button', { name: /Getac UX10 G3/i }).click();

  await expect(page).toHaveURL(/\/product\/getac-ux10-g3/);
  await expect(page.locator('.product-summary')).toContainText(/Getac UX10 G3/);
  await expectBlazorErrorHidden(page);
});

test('home chat gives four validated coffee machine scales before moving to results', async ({ page }) => {
  await registerUser(page);

  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.locator('#prompt').fill('Maquinas de Cafe da me os url para comprar, 4 opcoes custo beneficio, topo, mais barato intermedio');
  await page.locator('.search-send').click();

  await expect(page).toHaveURL(/\/$/);
  const assistant = page.locator('.home-chat-message-assistant').last();
  await expect(assistant).toContainText(/4 opcoes/i);
  await expect(assistant).toContainText(/Mais barata/i);
  await expect(assistant).toContainText(/Intermedia/i);
  await expect(assistant).toContainText(/Melhor custo\/beneficio/i);
  await expect(assistant).toContainText(/Topo/i);
  await expect(assistant).toContainText(/Escolha recomendada/i);
  await expect(assistant).toContainText(/https:\/\/www\.worten\.pt\/produtos\/maquina-de-cafe/i);
  await expect(page.locator('.home-chat-product')).toHaveCount(4);
  await expect(page.locator('.home-chat-products')).toContainText(/Essenza Mini/i);
  await expect(page.locator('.home-chat-products')).toContainText(/Magnifica Start/i);
  await expect(page.locator('.home-chat-products')).not.toContainText(/PlayStation/i);
  await expect(page).not.toHaveURL(/\/results|\/checkout|\/criteria/);
  await expectBlazorErrorHidden(page);

  await page.getByRole('button', { name: /Magnifica Start/i }).click();

  await expect(page).toHaveURL(/\/results\?query=/);
  await expect(page.locator('.results-empty-state')).toHaveCount(0);
  await expect(page.locator('.results-ai-panel')).toContainText(/Magnifica Start/i);
});

test('checkout for product without validated offer never invents a store', async ({ page }) => {
  await page.goto('/checkout?product=iphone-15-pro');

  await expect(page.locator('.checkout-empty-state')).toBeVisible();
  await expect(page.locator('.checkout-empty-state')).toContainText(/Sem loja validada|No validated store/i);
  await expect(page.locator('.seller-table')).toHaveCount(0);
  await expect(page.locator('.order-panel')).toHaveCount(0);
  await expect(page.locator('.checkout-empty-state')).not.toContainText(/Amazon\.es|Best Buy|Walmart|Newegg/i);
  await expect(page.getByRole('link', { name: /Ver loja|Open store/i })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /Pesquisar no KuantoKusta|Search on KuantoKusta/i })).toHaveAttribute(
    'href',
    /kuantokusta\.pt\/search\?q=iPhone%2015%20Pro/i
  );
});

test('protected history redirects anonymous users to login', async ({ page }) => {
  await page.goto('/history');

  await expect(page).toHaveURL(/\/login/);
  await expect(page.locator('body')).toContainText(/Entrar|Sign in|Login/i);
});

test('registered password account can login after logout', async ({ page }) => {
  const email = `auth-${Date.now()}-${Math.round(Math.random() * 100000)}@example.test`;
  const password = 'AuthCheck123!';

  await page.goto('/register?returnUrl=%2F');
  await page.waitForLoadState('networkidle');
  const registerEmail = page.locator('input[name="Email"]');
  const registerPassword = page.locator('input[name="Password"]');
  const registerConfirmPassword = page.locator('input[name="ConfirmPassword"]');
  await registerEmail.fill(email);
  await registerPassword.fill(password);
  await registerConfirmPassword.fill(password);
  await expect(registerEmail).toHaveValue(email);
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/auth/register') && response.status() === 302),
    page.locator('form.auth-form button[type="submit"]').click()
  ]);
  await expect(page).toHaveURL(/\/$/);

  await expect(page.locator('.topbar-actions')).toContainText(/Logout/i);

  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/auth/logout') && response.status() === 302),
    page.locator('form[action="/auth/logout"] button[type="submit"]').click()
  ]);
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.topbar-actions')).toContainText(/Login/i);

  await page.goto('/login?returnUrl=%2F');
  await page.waitForLoadState('networkidle');
  const loginEmail = page.locator('input[name="Email"]');
  const loginPassword = page.locator('input[name="Password"]');
  await loginEmail.fill(email);
  await loginPassword.fill(password);
  await expect(loginEmail).toHaveValue(email);
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/auth/login') && response.status() === 302),
    page.locator('form.auth-form button[type="submit"]').click()
  ]);
  await expect(page).toHaveURL(/\/$/);

  await expect(page.locator('.topbar-actions')).toContainText(/Logout/i);
  await expect(page.locator('.form-message')).toHaveCount(0);
});

test('history new search action is aligned with the page header', async ({ page }) => {
  await registerUser(page);

  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.locator('#prompt').fill('iPhone 17 Pro');
  await page.locator('.search-send').click();
  await expect(page).toHaveURL(/\/results\?query=/);

  await page.goto('/history');
  await expect(page.locator('.history-header')).toBeVisible();
  await expect(page.locator('.activity-card').first()).toBeVisible();
  await expect(page.getByRole('link', { name: /Nova pesquisa|New search/i })).toHaveAttribute('href', '/');

  const layout = await page.evaluate(() => {
    const header = document.querySelector('.history-header')?.getBoundingClientRect();
    const title = document.querySelector('.history-header h1')?.getBoundingClientRect();
    const button = document.querySelector('.history-new-search')?.getBoundingClientRect();
    const firstCard = document.querySelector('.activity-card')?.getBoundingClientRect();

    return {
      buttonRightAligned: Boolean(header && title && button && button.left > title.left && button.right <= header.right + 1),
      buttonInsideHeader: Boolean(header && button && button.top >= header.top - 1 && button.bottom <= header.bottom + 1),
      cardStartsAfterHeader: Boolean(header && firstCard && firstCard.top > header.bottom),
      horizontal: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
    };
  });

  expect(layout.buttonRightAligned).toBeTruthy();
  expect(layout.buttonInsideHeader).toBeTruthy();
  expect(layout.cardStartsAfterHeader).toBeTruthy();
  expect(layout.horizontal).toBe(false);
});

test('tutorial cards open a real local video player', async ({ page }) => {
  await page.goto('/tutorials');
  await page.waitForLoadState('networkidle');

  const firstTutorial = page.locator('.tutorial-card').first();
  await expect(firstTutorial).toContainText(/Como criar o primeiro prompt|Create the first prompt/i);
  await expect(firstTutorial.locator('video source')).toHaveAttribute('src', /tutorials\/primeiro-prompt\.webm/);

  const response = await page.request.get('/tutorials/primeiro-prompt.webm');
  expect(response.ok()).toBeTruthy();
  expect(response.headers()['content-type']).toContain('video/webm');
  expect(Number(response.headers()['content-length'] ?? '0')).toBeGreaterThan(100000);

  await firstTutorial.click();

  const dialog = page.locator('.tutorial-player-backdrop');
  await expect(dialog).toBeVisible();
  await expect(dialog.locator('video.tutorial-video')).toHaveAttribute('controls', '');
  await expect(dialog.locator('video.tutorial-video source')).toHaveAttribute('src', /tutorials\/primeiro-prompt\.webm/);
  await expect(page.locator('#blazor-error-ui')).not.toBeVisible();
});

test('image upload shows preview and can be removed', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);
  const onePixelPng = Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=',
    'base64');

  await page.setInputFiles('#product-image', {
    name: 'produto.png',
    mimeType: 'image/png',
    buffer: onePixelPng
  });

  await expect(page.locator('.image-suggestion-card img')).toBeVisible();
  await expect(page.locator('.image-remove-button')).toBeVisible();

  await page.locator('.image-remove-button').click();

  await expect(page.locator('.image-suggestion-card')).toHaveCount(0);
});

test('invalid image upload is blocked with a clear message', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);

  await page.setInputFiles('#product-image', {
    name: 'produto.gif',
    mimeType: 'image/gif',
    buffer: Buffer.from('not-a-supported-image')
  });

  await expect(page.locator('.quota-message')).toContainText(/inválido|invalid/i);
  await expect(page.locator('.image-suggestion-card img')).toHaveCount(0);
});

test('each home category suggestion returns sensible products with validated stores', async ({ page }) => {
  test.setTimeout(240000);
  await registerUser(page);

  const expectedCategories = [
    'Smartphones premium',
    'Telemóveis e smartwatches',
    'Smartphones e acessórios',
    'Carregadores e cabos',
    'Informática e portáteis',
    'Computadores e tablets',
    'Ratos e periféricos até 50€',
    'Armazenamento externo',
    'Monitores',
    'Impressoras',
    'Imagem, TV e Som',
    'Gaming e consolas',
    'Gaming',
    'Jogos e brinquedos',
    'Fotografia, drones e vídeo',
    'Eletrodomésticos',
    'Grandes eletrodomésticos',
    'Pequenos eletrodomésticos',
    'Máquinas de lavar',
    'Frigoríficos',
    'Ventoinhas',
    'Máquinas de café',
    'Microondas compactos',
    'Preparação de alimentos',
    'Aspiradores',
    'Beleza e saúde',
    'Saúde, beleza e perfumaria',
    'Animais de estimação',
    'Bebé',
    'Bebé, puericultura e brinquedos',
    'Bricolage',
    'Bricolagem e construção',
    'Casa e decoração',
    'Sofás',
    'Bricolage e jardim',
    'Jardim',
    'Desporto, outdoor e viagem',
    'Desporto',
    'Fitness',
    'Mobilidade',
    'Moda e acessórios',
    'Auto e moto',
    'Escritório e papelaria',
    'Cultura, lazer e livros',
    'Livros, música e filmes',
    'Gastronomia e vinhos',
    'Recondicionados e outlet',
    'Consolas PlayStation'
  ];
  const expectedTerms = {
    'Smartphones premium': ['iphone', 'galaxy', 'smartphone'],
    'Telemóveis e smartwatches': ['iphone', 'smartphone', 'galaxy', 'watch'],
    'Smartphones e acessórios': ['iphone', 'smartphone', 'carregador', 'galaxy'],
    'Carregadores e cabos': ['carregador', 'usb-c', '20w'],
    'Informática e portáteis': ['asus', 'vivobook', 'portatil'],
    'Computadores e tablets': ['asus', 'lenovo', 'tablet', 'portatil'],
    'Ratos e periféricos até 50€': ['rato', 'logitech', 'mouse'],
    'Armazenamento externo': ['disco', 'externo', 'passport'],
    'Monitores': ['monitor', 'ultragear', 'lg'],
    'Impressoras': ['impressora', 'hp', 'deskjet'],
    'Imagem, TV e Som': ['tv', 'smart tech', 'som'],
    'Gaming e consolas': ['playstation', 'ps5', 'consola'],
    'Gaming': ['playstation', 'ps5', 'consola'],
    'Jogos e brinquedos': ['lego', 'brinquedo', 'jogo'],
    'Fotografia, drones e vídeo': ['canon', 'drone', 'fotografia'],
    'Eletrodomésticos': ['bosch', 'frigor', 'microondas', 'balanca', 'xiaomi', 'beko', 'secar'],
    'Grandes eletrodomésticos': ['bosch', 'frigor', 'combinado', 'beko', 'secar'],
    'Pequenos eletrodomésticos': ['microondas', 'teka', 'cafe', 'balanca', 'xiaomi'],
    'Máquinas de lavar': ['becken', 'lavar', 'roupa'],
    'Frigoríficos': ['bosch', 'frigor', 'combinado'],
    'Ventoinhas': ['rowenta', 'ventoinha', 'ventilacao'],
    'Máquinas de café': ['nespresso', 'café', 'cafe'],
    'Microondas compactos': ['microondas', 'teka', '20'],
    'Preparação de alimentos': ['kenwood', 'chef', 'robot'],
    'Aspiradores': ['aspirador', 'becken', 'limpeza'],
    'Beleza e saúde': ['perfume', 'lattafa', 'beleza'],
    'Saúde, beleza e perfumaria': ['perfume', 'lattafa', 'beleza'],
    'Animais de estimação': ['royal', 'advance', 'racao', 'gato', 'cao'],
    'Bebé': ['fraldas', 'rascals', 'bebe'],
    'Bebé, puericultura e brinquedos': ['fraldas', 'lego', 'bebe'],
    'Bricolage': ['bosch', 'berbequim', 'bricolage', 'karcher', 'pressao'],
    'Bricolagem e construção': ['bosch', 'berbequim', 'bricolagem', 'karcher', 'pressao'],
    'Casa e decoração': ['cadeira', 'mitsai', 'casa'],
    'Sofás': ['sofa', 'homcom', 'cama'],
    'Bricolage e jardim': ['weber', 'jardim', 'barbecue'],
    'Jardim': ['weber', 'jardim', 'barbecue'],
    'Desporto, outdoor e viagem': ['bicicleta', 'otte', 'trotinete', 'fitness', 'passadeira'],
    'Desporto': ['bicicleta', 'otte', 'trotinete', 'fitness', 'passadeira'],
    'Fitness': ['fitfiu', 'fitness', 'passadeira'],
    'Mobilidade': ['trotinete', 'xiaomi', 'bicicleta'],
    'Moda e acessórios': ['adidas', 'sapatilhas', 'moda'],
    'Auto e moto': ['castrol', 'oleo', 'auto'],
    'Escritório e papelaria': ['hp', 'tinteiros', 'papelaria'],
    'Cultura, lazer e livros': ['livro', 'atomic', 'catan', 'papa'],
    'Livros, música e filmes': ['livro', 'atomic', 'habits'],
    'Gastronomia e vinhos': ['vinho', 'papa', 'figos'],
    'Recondicionados e outlet': ['playstation', 'ps5', 'consola'],
    'Consolas PlayStation': ['playstation', 'ps5']
  };

  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);
  const categories = (await page.locator('.category-menu button').allTextContents())
    .map((category) => category.trim());
  expect(categories).toEqual(expectedCategories);

  for (const category of categories) {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    const categoryButton = page.getByRole('button', { name: category, exact: true });
    await expect(categoryButton).toBeVisible();
    await categoryButton.click();
    await expect(page.locator('#prompt')).toHaveValue(new RegExp(category.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
    await expect(page.locator('.search-send')).toBeEnabled();
    await page.locator('.search-send').click();

    await expect(page).toHaveURL(/\/results\?query=/);
    await expect(page.locator('.topbar-market-indicator')).toContainText(/Portugal/);
    const firstCard = page.locator('.product-card').first();
    await expect(firstCard, `${category} should return at least one product card`).toBeVisible();

    const resultsText = (await page.locator('.results-ai-panel').innerText()).toLowerCase();
    expect(
      expectedTerms[category].some((term) => resultsText.includes(term.toLowerCase())),
      `${category} should return products related to the category`
    ).toBeTruthy();

    await firstCard.getByRole('link', { name: /Ver detalhes|Details/i }).click();
    await expect(page).toHaveURL(/\/product\//);
    await page.getByRole('link', { name: /Ver condições|Purchase conditions|Conditions/i }).click();

    await expect(page).toHaveURL(/\/checkout\?/);
    await expect(page.locator('.topbar-market-indicator')).toContainText(/Portugal/);
    await expect(page.locator('.checkout-empty-state'), `${category} should not end in empty checkout`).toHaveCount(0);
    await expect(page.locator('.seller-table tbody tr').first(), `${category} should show at least one validated store`).toBeVisible();
  }
});

test('generic microwave search returns a validated product and checkout store', async ({ page }) => {
  await page.goto('/results?query=microondas');
  await expect(page.locator('.results-empty-state')).toHaveCount(0);
  const firstCard = page.locator('.product-card').first();
  await expect(firstCard).toContainText(/Teka MW FS20|Microondas/i);

  await firstCard.getByRole('link', { name: /Ver detalhes|Details/i }).click();
  await expect(page).toHaveURL(/\/product\//);
  await page.getByRole('link', { name: /Ver condições|Purchase conditions|Conditions/i }).click();

  await expect(page).toHaveURL(/\/checkout\?/);
  await expect(page.locator('.checkout-empty-state')).toHaveCount(0);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/Darty|Castro Electronica|Worten|Aquario/i);
  await expect(page.getByRole('link', { name: /Ver loja|Open store/i }).first()).toHaveAttribute('target', '_blank');
});

test('dishwasher search never returns a washing machine and reaches a validated store', async ({ page }) => {
  await page.goto('/results?query=Maquina%20de%20Lavar%20Loica%20Indesit%20IN2FE13DT9S%2013%20conjuntos%20classe%20E');
  await expect(page.locator('.results-empty-state')).toHaveCount(0);
  const firstCard = page.locator('.product-card').first();
  await expect(firstCard).toContainText(/Indesit IN2FE13DT9S|Lavar Loica/i);
  await expect(firstCard).not.toContainText(/Lavar Roupa|Lavadora|Boostwash/i);
  await expect(page.locator('.results-ai-panel')).not.toContainText(/Becken Boostwash|Lavar Roupa/i);

  await firstCard.getByRole('link', { name: /Ver detalhes|Details/i }).click();
  await expect(page).toHaveURL(/\/product\/indesit-in2fe13dt9s-lava-loica/);
  await expect(page.locator('.product-summary')).toContainText(/Indesit IN2FE13DT9S/i);
  await page.locator('a[href^="/checkout?product=indesit-in2fe13dt9s-lava-loica"]').click();

  await expect(page).toHaveURL(/\/checkout\?/);
  await expect(page.locator('.checkout-empty-state')).toHaveCount(0);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/Castro Electronica/);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/236,19/);
  await expect(page.locator('a[href*="maquina-de-lavar-loica-in2fe13dt9s"]')).toHaveCount(2);
});

test('dryer search never returns fashion and reaches the validated KuantoKusta product page', async ({ page }) => {
  await page.goto('/results?query=Maquina%20de%20Secar%20Roupa%20Beko%20BM3T48249W%208Kg%20Classe%20C');
  await expect(page.locator('.results-empty-state')).toHaveCount(0);
  const firstCard = page.locator('.product-card').first();
  await expect(firstCard).toContainText(/Beko BM3T48249W|Secar Roupa/i);
  await expect(firstCard).not.toContainText(/Adidas|Sapatilhas/i);
  await expect(page.locator('.results-ai-panel')).not.toContainText(/Adidas|Sapatilhas/i);

  await firstCard.getByRole('link', { name: /Ver detalhes|Details/i }).click();
  await expect(page).toHaveURL(/\/product\/beko-bm3t48249w-maquina-secar-roupa/);
  await expect(page.locator('.product-summary')).toContainText(/Beko BM3T48249W/i);
  await page.locator('a[href^="/checkout?product=beko-bm3t48249w-maquina-secar-roupa"]').click();

  await expect(page).toHaveURL(/\/checkout\?product=beko-bm3t48249w-maquina-secar-roupa/);
  await expect(page.locator('.checkout-empty-state')).toHaveCount(0);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/KuantoKusta/);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/366,90/);
  await expect(page.locator('a[href*="kuantokusta.pt/p/11598597/beko-bm3t48249w-8kg-classe-c"]')).toHaveCount(2);
});

test('gaming chair search does not return a console and reaches a validated store', async ({ page }) => {
  await page.goto('/results?query=cadeira%20gaming');
  await expect(page.locator('.results-empty-state')).toHaveCount(0);
  await expect(page.locator('.results-ai-panel')).toContainText(/RACINGREAT Costas Altas Cadeira Gaming/i);
  await expect(page.locator('.results-ai-panel')).toContainText(/65,00/);
  await expect(page.locator('.results-ai-panel')).not.toContainText(/PlayStation 5 Slim/i);

  await page.locator('a[href^="/product/racingreat-costas-altas-cadeira-gaming"]').first().click();
  await expect(page).toHaveURL(/\/product\/racingreat-costas-altas-cadeira-gaming/);
  await expect(page.locator('.product-summary')).toContainText(/RACINGREAT Costas Altas Cadeira Gaming/i);
  await expect(page.locator('a[href^="/checkout?product=racingreat-costas-altas-cadeira-gaming"]')).toHaveCount(1);

  await page.locator('a[href^="/checkout?product=racingreat-costas-altas-cadeira-gaming"]').click();
  await expect(page).toHaveURL(/\/checkout\?product=racingreat-costas-altas-cadeira-gaming/);
  await expect(page.locator('.checkout-empty-state')).toHaveCount(0);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/Worten/);
  await expect(page.locator('.seller-table tbody tr').first()).toContainText(/65,00/);
  await expect(page.locator('a[href*="worten.pt/produtos/cadeira-de-escritorio-ergonomica-racingreat"]')).toHaveCount(2);
});

test('main pages do not create horizontal document overflow on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });

  for (const path of ['/', '/results?query=rato%20ate%2050%20euros', '/checkout?product=iphone-15-pro']) {
    await page.goto(path);
    const hasOverflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    expect(hasOverflow, `${path} should not overflow horizontally`).toBe(false);
  }
});

test('product detail compact desktop layout avoids document scroll', async ({ page }) => {
  for (const viewport of [{ width: 1366, height: 768 }, { width: 1692, height: 900 }]) {
    await page.setViewportSize(viewport);

    for (const path of [
      '/product/iphone-17-pro?query=Smartphones%20premium',
      '/product/getac-ux10-g3?query=tablet%20industrial'
    ]) {
      await page.goto(path);
      await expect(page.locator('.product-detail-grid')).toBeVisible();

      const layout = await page.evaluate(() => ({
        vertical: document.documentElement.scrollHeight > document.documentElement.clientHeight + 1,
        horizontal: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
      }));

      expect(layout.vertical, `${path} should fit ${viewport.width}x${viewport.height} without vertical document scroll`).toBe(false);
      expect(layout.horizontal, `${path} should not overflow horizontally`).toBe(false);
    }
  }
});

test('checkout compact desktop layout avoids document scroll', async ({ page }) => {
  for (const viewport of [{ width: 1366, height: 768 }, { width: 1692, height: 900 }]) {
    await page.setViewportSize(viewport);

    for (const path of [
      '/checkout?product=iphone-17-pro&query=Smartphones%20premium',
      '/checkout?product=logitech-g305-lightspeed&query=rato%20ate%2050%20euros'
    ]) {
      await page.goto(path);
      await expect(page.locator('.checkout-grid')).toBeVisible();
      await expect(page.getByRole('link', { name: /Voltar ao produto|Back to product/i })).toHaveAttribute(
        'href',
        /\/product\//
      );

      const layout = await page.evaluate(() => ({
        vertical: document.documentElement.scrollHeight > document.documentElement.clientHeight + 1,
        horizontal: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
      }));

      expect(layout.vertical, `${path} should fit ${viewport.width}x${viewport.height} without vertical document scroll`).toBe(false);
      expect(layout.horizontal, `${path} should not overflow horizontally`).toBe(false);
    }
  }
});
