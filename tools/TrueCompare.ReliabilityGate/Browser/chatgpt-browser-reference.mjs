import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';

const args = parseArgs(process.argv.slice(2));
const promptFile = required(args, 'prompt-file');
const output = required(args, 'output');
const screenshot = args.screenshot;
const requestedModel = args.model ?? 'GPT-5.5';
const requestedReasoning = args.reasoning ?? 'Alta';
const prompt = await fs.readFile(promptFile, 'utf8');

let browser;
let context;
let page;
let ownsBrowser = true;

try {
  ({ browser, context, page, ownsBrowser } = await openChatGptBrowser(args));
  await page.goto('https://chatgpt.com/pt-PT/', { waitUntil: 'domcontentloaded', timeout: 60_000 });
  await page.waitForTimeout(2500);

  if (await isLoginRequired(page)) {
    await capture(page, screenshot);
    await writeReference(output, {
      status: 'blocked',
      promptSent: prompt,
      responseText: '',
      modelUsed: null,
      reasoningMode: null,
      timestamp: new Date().toISOString(),
      screenshotPath: screenshot ?? null,
      blockReason: 'ChatGPT no browser pediu login. A validação live exige sessão Pro já autenticada no Chrome.',
      recommendedProductNames: []
    });
    process.exit(0);
  }

  const temporaryEnabled = await ensureTemporaryOrCleanChat(page);
  if (!temporaryEnabled) {
    await capture(page, screenshot);
    await writeReference(output, {
      status: 'inconclusive',
      promptSent: prompt,
      responseText: '',
      modelUsed: await detectModelText(page),
      reasoningMode: null,
      timestamp: new Date().toISOString(),
      screenshotPath: screenshot ?? null,
      blockReason: 'Não foi possível confirmar conversa temporária ou uma conversa nova limpa no ChatGPT visual.',
      recommendedProductNames: []
    });
    process.exit(0);
  }

  const model = await ensureModelAndReasoning(page, requestedModel, requestedReasoning);
  if (!model.modelConfirmed) {
    await capture(page, screenshot);
    await writeReference(output, {
      status: 'inconclusive',
      promptSent: prompt,
      responseText: '',
      modelUsed: model.modelText,
      reasoningMode: model.reasoningText,
      timestamp: new Date().toISOString(),
      screenshotPath: screenshot ?? null,
      blockReason: `Não foi possível confirmar o modelo pedido (${requestedModel}) ou o modo de raciocínio (${requestedReasoning}) na interface visual.`,
      recommendedProductNames: []
    });
    process.exit(0);
  }

  await sendPrompt(page, prompt);
  const responseText = await waitForAssistantResponse(page);
  await capture(page, screenshot);

  await writeReference(output, {
    status: responseText.trim().length > 0 ? 'validated' : 'inconclusive',
    promptSent: prompt,
    responseText,
    modelUsed: model.modelText,
    reasoningMode: model.reasoningText,
    timestamp: new Date().toISOString(),
    screenshotPath: screenshot ?? null,
    blockReason: responseText.trim().length > 0 ? null : 'ChatGPT não devolveu resposta capturável.',
    recommendedProductNames: extractRecommendedNames(responseText)
  });
} catch (error) {
  await capture(page, screenshot);
  await writeReference(output, {
    status: 'blocked',
    promptSent: prompt,
    responseText: '',
    modelUsed: page ? await detectModelText(page).catch(() => null) : null,
    reasoningMode: null,
    timestamp: new Date().toISOString(),
    screenshotPath: screenshot ?? null,
    blockReason: `Erro na automação visual do ChatGPT: ${error.message}`,
    recommendedProductNames: []
  });
} finally {
  if (context && !args.cdp) {
    await context.close().catch(() => {});
  }

  if (browser && ownsBrowser) {
    await browser.close().catch(() => {});
  }
}

async function openChatGptBrowser(parsed) {
  if (parsed.cdp) {
    const cdpBrowser = await chromium.connectOverCDP(parsed.cdp);
    const cdpContext = cdpBrowser.contexts()[0] ?? await cdpBrowser.newContext();
    const cdpPage = cdpContext.pages()[0] ?? await cdpContext.newPage();
    return { browser: cdpBrowser, context: cdpContext, page: cdpPage, ownsBrowser: false };
  }

  const userDataDir = parsed['user-data-dir'] ?? path.join(os.tmpdir(), `truecompare-chatgpt-${Date.now()}`);
  const persistentContext = await chromium.launchPersistentContext(userDataDir, {
    channel: 'chrome',
    headless: false,
    viewport: { width: 1440, height: 1000 },
    locale: 'pt-PT',
    extraHTTPHeaders: { 'Accept-Language': 'pt-PT,pt;q=0.9,en-US;q=0.7,en;q=0.6' }
  });
  const persistentPage = persistentContext.pages()[0] ?? await persistentContext.newPage();
  return { browser: null, context: persistentContext, page: persistentPage, ownsBrowser: true };
}

async function isLoginRequired(chatPage) {
  const text = await bodyText(chatPage);
  return /iniciar sessão|log in|sign in|sign up|criar conta|continue with google/i.test(text);
}

async function ensureTemporaryOrCleanChat(chatPage) {
  await clickAny(chatPage, [
    'a[href="/"]',
    'a[href="/pt-PT/"]',
    'button[aria-label*="Novo chat" i]',
    'button[aria-label*="New chat" i]',
    'text=Novo chat',
    'text=New chat'
  ]);

  await chatPage.waitForTimeout(1200);
  await clickAny(chatPage, [
    'text=Conversa temporária',
    'text=Chat temporário',
    'text=Temporary chat',
    'text=Temporary Chat',
    'button[aria-label*="tempor" i]',
    'button[aria-label*="temporary" i]'
  ]);

  await chatPage.waitForTimeout(1200);
  const text = await bodyText(chatPage);
  if (/conversa temporária|chat temporário|temporary chat/i.test(text)) {
    return true;
  }

  const input = await findPromptInput(chatPage);
  const messages = await chatPage.locator('[data-message-author-role], article, main [class*="message"]').count().catch(() => 1);
  return messages <= 1 && await input.count().catch(() => 0) > 0;
}

async function ensureModelAndReasoning(chatPage, modelName, reasoningName) {
  await clickAny(chatPage, [
    '[data-testid="model-switcher-dropdown-button"]',
    'button:has-text("GPT")',
    'button[aria-label*="modelo" i]',
    'button[aria-label*="model" i]'
  ]);
  await chatPage.waitForTimeout(900);

  const advancedModels = [
    modelName,
    'GPT-5.5',
    'GPT-5',
    'GPT-4.5',
    'o3',
    'o4',
    'GPT-4o'
  ];
  for (const candidate of advancedModels) {
    if (await clickText(chatPage, candidate)) {
      break;
    }
  }

  await chatPage.waitForTimeout(900);
  await clickAny(chatPage, [
    'text=Alta',
    'text=High',
    'text=Raciocínio alto',
    'text=High reasoning',
    'text=Inteligência alta'
  ]);

  const modelText = await detectModelText(chatPage);
  const body = await bodyText(chatPage);
  const modelConfirmed = modelText !== null
    && /gpt|o3|o4/i.test(modelText)
    && (/5\.5|5|4\.5|o3|o4/i.test(modelText) || /alta|high/i.test(body));
  const reasoningText = /alta|high/i.test(body) ? reasoningName : null;
  return { modelText, reasoningText, modelConfirmed };
}

async function sendPrompt(chatPage, text) {
  const input = await findPromptInput(chatPage);
  if (await input.count() === 0) {
    throw new Error('Não foi encontrada a caixa de prompt do ChatGPT.');
  }

  await input.first().click();
  await input.first().fill(text).catch(async () => {
    await chatPage.keyboard.insertText(text);
  });
  await chatPage.keyboard.press('Enter');
}

async function waitForAssistantResponse(chatPage) {
  const before = await chatPage.locator('[data-message-author-role="assistant"]').count().catch(() => 0);
  await chatPage.waitForFunction(
    previous => document.querySelectorAll('[data-message-author-role="assistant"]').length > previous,
    before,
    { timeout: 180_000 }
  ).catch(() => {});

  await chatPage.waitForLoadState('networkidle', { timeout: 30_000 }).catch(() => {});
  await chatPage.waitForTimeout(2500);

  for (let attempt = 0; attempt < 20; attempt += 1) {
    const assistantMessages = await chatPage.locator('[data-message-author-role="assistant"]').allTextContents().catch(() => []);
    const last = assistantMessages.map(value => value.trim()).filter(Boolean).at(-1) ?? '';
    if (last.length > 40 && !/pensando|thinking/i.test(last)) {
      return last;
    }

    await chatPage.waitForTimeout(1500);
  }

  return (await chatPage.locator('[data-message-author-role="assistant"]').allTextContents().catch(() => []))
    .map(value => value.trim())
    .filter(Boolean)
    .at(-1) ?? '';
}

async function findPromptInput(chatPage) {
  const selectors = [
    'textarea[data-testid="prompt-textarea"]',
    '[data-testid="prompt-textarea"]',
    '#prompt-textarea',
    'textarea',
    '[contenteditable="true"]'
  ];

  for (const selector of selectors) {
    const locator = chatPage.locator(selector).first();
    if (await locator.count().catch(() => 0)) {
      return locator;
    }
  }

  return chatPage.locator('__missing__');
}

async function detectModelText(chatPage) {
  const text = await bodyText(chatPage);
  const match = text.match(/GPT[-\s]?\d(?:\.\d)?(?:\.\d)?|GPT-4o|o3|o4/iu);
  return match?.[0] ?? null;
}

function extractRecommendedNames(responseText) {
  const jsonText = extractJsonObject(responseText);
  if (jsonText) {
    try {
      const parsed = JSON.parse(jsonText);
      return [
        parsed.mais_barata_aceitavel,
        parsed.melhor_custo_beneficio,
        parsed.intermedia,
        parsed.topo,
        parsed.evitar
      ].filter(value => typeof value === 'string' && value.trim().length > 0);
    } catch {
      return [];
    }
  }

  return [];
}

function extractJsonObject(value) {
  const first = value.indexOf('{');
  const last = value.lastIndexOf('}');
  return first >= 0 && last > first ? value.slice(first, last + 1) : null;
}

async function clickAny(chatPage, selectors) {
  for (const selector of selectors) {
    const locator = chatPage.locator(selector).first();
    if (await locator.count().catch(() => 0)) {
      await locator.click({ timeout: 3000 }).catch(() => {});
      return true;
    }
  }

  return false;
}

async function clickText(chatPage, text) {
  return clickAny(chatPage, [`text=${text}`, `button:has-text("${text}")`, `[role="menuitem"]:has-text("${text}")`]);
}

async function bodyText(chatPage) {
  return await chatPage.locator('body').innerText({ timeout: 10_000 }).catch(() => '');
}

async function capture(chatPage, file) {
  if (!chatPage || !file) {
    return;
  }

  await fs.mkdir(path.dirname(file), { recursive: true });
  await chatPage.screenshot({ path: file, fullPage: true }).catch(() => {});
}

async function writeReference(file, value) {
  await fs.mkdir(path.dirname(file), { recursive: true });
  await fs.writeFile(file, JSON.stringify(value, null, 2));
}

function parseArgs(argv) {
  const parsed = {};
  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (!arg.startsWith('--')) {
      continue;
    }

    const key = arg.slice(2);
    const next = argv[index + 1];
    parsed[key] = next && !next.startsWith('--') ? argv[++index] : 'true';
  }

  return parsed;
}

function required(parsed, key) {
  if (!parsed[key]) {
    throw new Error(`Argumento obrigatório em falta: --${key}`);
  }

  return parsed[key];
}
