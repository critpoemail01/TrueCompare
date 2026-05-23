import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';

const args = parseArgs(process.argv.slice(2));
const output = args.output ?? 'kuantokusta-live-catalog.json';
const maxProducts = Number(args['max-products'] ?? 20);
const categoryFilter = normalize(args.category ?? '');
const evidenceDir = path.join(path.dirname(output), 'kuantokusta-evidence');

await fs.mkdir(evidenceDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({
  locale: 'pt-PT',
  extraHTTPHeaders: { 'Accept-Language': 'pt-PT,pt;q=0.9' }
});

try {
  await page.goto('https://www.kuantokusta.pt/', { waitUntil: 'domcontentloaded', timeout: 45_000 });
  await page.screenshot({ path: path.join(evidenceDir, 'homepage.png'), fullPage: true }).catch(() => {});
  const bodyText = await safeBodyText(page);
  if (/access denied|permission to access/i.test(bodyText)) {
    await writeJson(output, {
      categories: [],
      status: 'blocked',
      blockReason: 'KuantoKusta devolveu Access Denied ao browser automatizado.',
      capturedAt: new Date().toISOString()
    });
    await browser.close();
    process.exit(0);
  }

  const categories = await discoverCategoryLinks(page);
  const selected = categoryFilter
    ? categories.filter(category => normalize(category.name).includes(categoryFilter))
    : categories;

  const collected = [];
  for (const category of selected) {
    const subcategories = await collectSubcategories(page, category, maxProducts);
    collected.push({ ...category, subcategories });
    await delay(900);
  }

  await writeJson(output, {
    categories: collected,
    status: 'validated',
    capturedAt: new Date().toISOString()
  });
} catch (error) {
  await writeJson(output, {
    categories: [],
    status: 'blocked',
    blockReason: `Erro de navegação KuantoKusta: ${error.message}`,
    capturedAt: new Date().toISOString()
  });
} finally {
  await browser.close();
}

async function discoverCategoryLinks(page) {
  await clickFirst(page, ['button:has-text("Menu")', 'button[aria-label*="menu" i]', 'text=MENU']).catch(() => {});
  await page.waitForTimeout(1000);

  const links = await page.locator('a[href], button').evaluateAll(elements => {
    return elements
      .map(element => ({
        name: (element.innerText || element.getAttribute('aria-label') || '').trim(),
        url: element.href || element.getAttribute('data-href') || ''
      }))
      .filter(item => item.name.length > 2)
      .slice(0, 300);
  });

  const seen = new Set();
  return links
    .filter(item => /\/c\/|\/cat\/|\/categoria|\/s\//i.test(item.url) || /^[A-ZÁÀÉÍÓÚÂÊÔÃÕÇ][\w\sÁÀÉÍÓÚÂÊÔÃÕÇ,&-]{3,}$/.test(item.name))
    .filter(item => {
      const key = normalize(item.name);
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    })
    .slice(0, 80)
    .map(item => ({ name: item.name, url: item.url || 'https://www.kuantokusta.pt/' }));
}

async function collectSubcategories(page, category, maxProducts) {
  if (category.url && category.url.startsWith('http')) {
    await page.goto(category.url, { waitUntil: 'domcontentloaded', timeout: 35_000 }).catch(() => {});
  }

  await page.waitForTimeout(800);
  const htmlPath = path.join(evidenceDir, `${slug(category.name)}.html`);
  await fs.writeFile(htmlPath, await page.content()).catch(() => {});
  await page.screenshot({ path: path.join(evidenceDir, `${slug(category.name)}.png`), fullPage: true }).catch(() => {});

  const subLinks = await page.locator('a[href]').evaluateAll(elements => {
    return elements
      .map(element => ({ name: (element.innerText || '').trim(), url: element.href || '' }))
      .filter(item => item.name.length > 2 && item.url);
  }).catch(() => []);

  const uniqueSubcategories = dedupeByName(subLinks)
    .filter(item => item.url.includes('kuantokusta.pt'))
    .slice(0, 30);

  const directProducts = await collectProductsFromCurrentPage(page, maxProducts);
  if (uniqueSubcategories.length === 0) {
    return [{
      category: category.name,
      name: category.name,
      url: page.url(),
      products: directProducts,
      status: directProducts.length > 0 ? 'validated' : 'notValidable',
      notValidableReason: directProducts.length > 0 ? null : 'Não foram detetados produtos na página.',
      htmlSnapshotPath: htmlPath,
      screenshotPath: path.join(evidenceDir, `${slug(category.name)}.png`)
    }];
  }

  const subcategories = [];
  for (const sub of uniqueSubcategories.slice(0, 12)) {
    await page.goto(sub.url, { waitUntil: 'domcontentloaded', timeout: 35_000 }).catch(() => {});
    await page.waitForTimeout(700);
    const products = await collectProductsFromCurrentPage(page, maxProducts);
    const subHtml = path.join(evidenceDir, `${slug(category.name)}-${slug(sub.name)}.html`);
    const subPng = path.join(evidenceDir, `${slug(category.name)}-${slug(sub.name)}.png`);
    await fs.writeFile(subHtml, await page.content()).catch(() => {});
    await page.screenshot({ path: subPng, fullPage: true }).catch(() => {});
    subcategories.push({
      category: category.name,
      name: sub.name,
      url: sub.url,
      products,
      status: products.length > 0 ? 'validated' : 'notValidable',
      notValidableReason: products.length > 0 ? null : 'Subcategoria sem produtos extraíveis por seletores estáveis.',
      htmlSnapshotPath: subHtml,
      screenshotPath: subPng
    });
    await delay(900);
  }

  return subcategories;
}

async function collectProductsFromCurrentPage(page, maxProducts) {
  return await page.locator('a[href*="/p/"], article, [data-testid*="product" i], .product-card').evaluateAll((elements, max) => {
    const products = [];
    for (const element of elements) {
      const text = (element.innerText || '').trim();
      const link = element.href || element.querySelector?.('a[href*="/p/"]')?.href || '';
      const priceMatch = text.match(/(?:desde\s*)?(\d{1,3}(?:[.\s]\d{3})*,\d{2})\s*€/i);
      const lines = text.split('\n').map(line => line.trim()).filter(Boolean);
      const name = lines.find(line => !/desde|€|boa compra|mais vendido|favorito|loja|rating/i.test(line)) || lines[0] || '';
      if (!name || !priceMatch) continue;
      products.push({
        name,
        brand: name.split(' ')[0] || '',
        model: name,
        minPriceCents: parseEuroCents(priceMatch[1]),
        maxPriceCents: null,
        url: link,
        stores: Array.from(text.matchAll(/em\s+(\d+)\s+lojas?/gi)).map(match => `${match[1]} lojas`),
        badges: lines.filter(line => /boa compra|mais vendido|popular|favorito/i.test(line)),
        rating: (text.match(/★\s*([\d.,]+)/)?.[1]) || null,
        availability: /temporariamente indispon/i.test(text) ? 'Temporariamente indisponível' : null
      });
      if (products.length >= max) break;
    }
    return products;

    function parseEuroCents(value) {
      const normalized = value.replace(/\./g, '').replace(/\s/g, '').replace(',', '.');
      return Math.round(Number(normalized) * 100);
    }
  }, maxProducts).catch(() => []);
}

async function safeBodyText(page) {
  return await page.locator('body').innerText({ timeout: 10_000 }).catch(() => '');
}

async function clickFirst(page, selectors) {
  for (const selector of selectors) {
    const locator = page.locator(selector).first();
    if (await locator.count()) {
      await locator.click({ timeout: 3000 });
      return;
    }
  }
}

function dedupeByName(items) {
  const seen = new Set();
  return items.filter(item => {
    const key = normalize(item.name);
    if (!key || seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function parseArgs(argv) {
  const parsed = {};
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (!arg.startsWith('--')) continue;
    const key = arg.slice(2);
    const next = argv[i + 1];
    parsed[key] = next && !next.startsWith('--') ? argv[++i] : 'true';
  }
  return parsed;
}

function normalize(value) {
  return String(value || '').normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase().trim();
}

function slug(value) {
  return normalize(value).replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 80) || 'item';
}

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function writeJson(file, value) {
  await fs.mkdir(path.dirname(file), { recursive: true });
  await fs.writeFile(file, JSON.stringify(value, null, 2));
}
