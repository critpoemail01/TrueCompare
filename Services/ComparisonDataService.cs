using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class ComparisonDataService(AppText text)
{
    private const string LaptopCatalog = "laptops";
    private const string SmartphoneCatalog = "smartphones";
    private const string ChargerCatalog = "chargers";
    private const string ApplianceCatalog = "appliances";
    private const string MouseCatalog = "mice";
    private const string RuggedTabletCatalog = "rugged-tablets";
    private const string TabletCatalog = "tablets";
    private const string StorageCatalog = "storage";
    private const string MonitorCatalog = "monitors";
    private const string KeyboardCatalog = "keyboards";
    private const string HeadphonesCatalog = "headphones";
    private const string TvCatalog = "tvs";
    private const string CameraCatalog = "cameras";
    private const string DroneCatalog = "drones";
    private const string CoffeeMachineCatalog = "coffee-machines";
    private const string ToolCatalog = "tools";
    private const string ChairCatalog = "chairs";
    private const string TyreCatalog = "tyres";
    private const string PrinterCatalog = "printers";
    private const string PetFoodCatalog = "pet-food";
    private const string BabyCareCatalog = "baby-care";
    private const string ToyCatalog = "toys";
    private const string HealthBeautyCatalog = "health-beauty";
    private const string HomeCatalog = "home";
    private const string SportCatalog = "sports";
    private const string AutoCatalog = "auto";
    private const string OfficeCatalog = "office";
    private const string CultureFoodCatalog = "culture-food";
    private const string FashionCatalog = "fashion";
    private const string MarketplaceCatalog = "marketplace";
    private const string GeneralCatalog = "general";

    private static readonly string[] SmartphoneCatalogTerms =
    [
        "smartphone",
        "smartphones",
        "telemovel",
        "phone",
        "phones",
        "mobile",
        "mobiles",
        "iphone",
        "ipohone",
        "android",
        "pixel",
        "redmi",
        "poco",
        "dual sim",
        "5g",
        "galaxy",
        "celular",
        "senior",
        "spc",
        "maxcom"
    ];

    private static readonly string[] ChargerCatalogTerms =
    [
        "carregador",
        "carregadores",
        "charger",
        "chargers",
        "adaptador",
        "adaptadores",
        "adaptador usb-c",
        "usb-c charger",
        "carregador usb-c",
        "carregador iphone",
        "carregador de iphone",
        "cabo iphone",
        "cabo lightning",
        "lightning",
        "magsafe",
        "powerbank",
        "power bank",
        "bateria externa"
    ];

    private static readonly string[] ApplianceCatalogTerms =
    [
        "eletrodomestico",
        "eletrodomesticos",
        "electrodomestico",
        "electrodomesticos",
        "appliance",
        "appliances",
        "frigorifico",
        "fridge",
        "geladeira",
        "combinado",
        "maquina de lavar",
        "maquina lavar",
        "lavadora",
        "lavagem",
        "lava loica",
        "lava-loica",
        "dishwasher",
        "washer",
        "eficiencia a",
        "classe a",
        "a+++",
        "cozinha",
        "forno",
        "fornos",
        "microondas",
        "placa",
        "placas",
        "exaustor",
        "exaustores",
        "balanca",
        "balancas",
        "fritadeira",
        "fritadeiras",
        "torradeira",
        "torradeiras",
        "jarro eletrico",
        "robot de cozinha",
        "robots de cozinha"
    ];

    private static readonly string[] LaptopCatalogTerms =
    [
        "laptop",
        "laptops",
        "portatil",
        "portateis",
        "notebook",
        "computador",
        "macbook",
        "thinkpad",
        "zenbook",
        "xps",
        "ultrabook",
        "pc profissional",
        "chromebook",
        "ideapad",
        "vivobook",
        "aspire",
        "intel core",
        "core ultra",
        "ryzen",
        "celeron",
        "windows 11",
        "macos",
        "rtx",
        "gaming"
    ];

    private static readonly string[] MouseCatalogTerms =
    [
        "rato",
        "ratos",
        "mouse",
        "mice",
        "wireless mouse",
        "bluetooth mouse",
        "gaming mouse",
        "rato gaming",
        "dpi"
    ];

    private static readonly string[] RuggedTabletCatalogTerms =
    [
        "tablet industrial",
        "tablet rugged",
        "tablete industrial",
        "tabela industrial",
        "touch industrial",
        "rugged",
        "robusto",
        "robusta",
        "industrial",
        "industria",
        "fabrica",
        "chao de fabrica",
        "todo terreno",
        "todoterreno",
        "ip65",
        "ip66",
        "ip68",
        "mil-std"
    ];

    private static readonly string[] TabletCatalogTerms =
    [
        "tablet",
        "tablets",
        "ipad",
        "galaxy tab",
        "lenovo tab",
        "idea tab",
        "tab a",
        "wifi tablet",
        "wi-fi tablet",
        "11 polegadas",
        "10 polegadas"
    ];

    private static readonly string[] StorageCatalogTerms =
    [
        "disco externo",
        "discos externos",
        "external drive",
        "external hard drive",
        "hdd externo",
        "ssd externo",
        "armazenamento externo",
        "my passport",
        "expansion portable",
        "usb 3.2"
    ];

    private static readonly string[] MonitorCatalogTerms =
    [
        "monitor",
        "monitores",
        "monitor gaming",
        "monitor pc",
        "display",
        "qhd",
        "144hz",
        "165hz",
        "27 polegadas",
        "27\""
    ];

    private static readonly string[] KeyboardCatalogTerms =
    [
        "teclado",
        "teclados",
        "keyboard",
        "keyboards",
        "teclado mecanico",
        "teclado sem fios",
        "mx keys",
        "keychron"
    ];

    private static readonly string[] HeadphonesCatalogTerms =
    [
        "auscultadores",
        "auriculares",
        "headphones",
        "headset",
        "earbuds",
        "airpods",
        "galaxy buds",
        "buds",
        "soundbar",
        "coluna",
        "colunas",
        "noise cancelling",
        "anc"
    ];

    private static readonly string[] TvCatalogTerms =
    [
        "televisor",
        "televisao",
        "televisão",
        "smart tv",
        "suporte tv",
        "suporte de tv",
        "suporte de parede",
        "suporte chao",
        "vesa",
        "ecras",
        "ecrãs",
        "tv",
        "oled",
        "qled",
        "mini led",
        "55 polegadas",
        "55\""
    ];

    private static readonly string[] CameraCatalogTerms =
    [
        "camera",
        "camara",
        "câmara",
        "maquina fotografica",
        "máquina fotográfica",
        "fotografica",
        "fotográfica",
        "canon eos",
        "fujifilm",
        "instax",
        "gopro",
        "sony alpha"
    ];

    private static readonly string[] DroneCatalogTerms =
    [
        "drone",
        "drones",
        "dji",
        "mini drone",
        "drone gps",
        "avata",
        "fly more"
    ];

    private static readonly string[] CoffeeMachineCatalogTerms =
    [
        "maquina de cafe",
        "maquina café",
        "máquina de café",
        "maquinas de cafe",
        "coffee machine",
        "espresso",
        "nespresso",
        "delonghi",
        "magnifica"
    ];

    private static readonly string[] ToolCatalogTerms =
    [
        "berbequim",
        "berbequins",
        "aparafusadora",
        "aparafusadoras",
        "drill",
        "ferramenta",
        "ferramentas",
        "bosch professional",
        "makita",
        "dewalt",
        "18v"
    ];

    private static readonly string[] ChairCatalogTerms =
    [
        "cadeira",
        "cadeiras",
        "chair",
        "chairs",
        "cadeira escritorio",
        "cadeira escritório",
        "cadeira ergonomica",
        "cadeira ergonómica",
        "apoio lombar"
    ];

    private static readonly string[] TyreCatalogTerms =
    [
        "pneu",
        "pneus",
        "tyre",
        "tyres",
        "205 55 r16",
        "205/55 r16",
        "primacy",
        "premiumcontact",
        "turanza"
    ];

    private static readonly string[] PrinterCatalogTerms =
    [
        "impressora",
        "impressoras",
        "printer",
        "printers",
        "multifuncoes",
        "multifunções",
        "ecotank",
        "officejet",
        "laser",
        "tinteiro",
        "tinteiros",
        "toner",
        "cartucho",
        "cartuchos",
        "consumiveis",
        "consumiveis impressao"
    ];

    private static readonly string[] PetFoodCatalogTerms =
    [
        "racao",
        "ração",
        "comida cao",
        "comida cão",
        "pet food",
        "royal canin",
        "purina",
        "ownat",
        "acana",
        "orijen",
        "advance",
        "advance mini",
        "advance vet",
        "vet diets",
        "veterinario",
        "veterinarios",
        "frontpro",
        "omnicondro",
        "wejoint",
        "kimimove",
        "hifarmax",
        "kimipharma",
        "hypoallergenic",
        "dog",
        "cat",
        "complet chicken",
        "adult chicken",
        "cao",
        "cão",
        "gato"
    ];

    private static readonly string[] BabyCareCatalogTerms =
    [
        "fralda",
        "fraldas",
        "pampers",
        "dodot",
        "bebe",
        "bebé",
        "baby",
        "chicco",
        "carrinho bebe",
        "berco bebe"
    ];

    private static readonly string[] ToyCatalogTerms =
    [
        "lego",
        "brinquedo",
        "brinquedos",
        "boneca",
        "bonecas",
        "jogo educativo",
        "puzzle",
        "playmobil",
        "hot wheels",
        "minecraft",
        "star wars",
        "nenuco",
        "pinypon",
        "monster high",
        "furby",
        "hasbro"
    ];

    private static readonly string[] HealthBeautyCatalogTerms =
    [
        "saude",
        "beleza",
        "comprimidos",
        "capsulas",
        "saquetas",
        "suplemento",
        "magnesio",
        "omega",
        "creme",
        "serum",
        "fisiocrem",
        "perfume",
        "eau de parfum",
        "eau de toilette",
        "shampoo",
        "protetor solar",
        "hidratante"
    ];

    private static readonly string[] HomeCatalogTerms =
    [
        "casa",
        "decoracao",
        "mesa",
        "mesas",
        "desk",
        "secretaria",
        "cadeira",
        "cadeiras",
        "sofa",
        "colchao",
        "candeeiro",
        "tapete",
        "movel",
        "moveis",
        "estante",
        "cirio",
        "medalha",
        "pulseira",
        "trem de cozinha",
        "trem cozinha",
        "silampos",
        "loica de cozinha",
        "louca de cozinha"
    ];

    private static readonly string[] SportCatalogTerms =
    [
        "desporto",
        "fitness",
        "bicicleta",
        "bicicletas",
        "trotinete",
        "trotinetes",
        "passadeira",
        "garmin",
        "forerunner",
        "bola",
        "capacete",
        "treino",
        "sapatilhas",
        "tenis",
        "running",
        "trail",
        "asics",
        "adidas",
        "nike"
    ];

    private static readonly string[] AutoCatalogTerms =
    [
        "auto",
        "moto",
        "pneu",
        "pneus",
        "oleo motor",
        "bateria auto",
        "capacete moto",
        "escovas limpa vidros",
        "filtro oleo",
        "acessorios automovel",
        "5w30",
        "5w-30",
        "10w40",
        "10w-40",
        "mannol",
        "liqui moly",
        "castrol",
        "elf",
        "klima refresh"
    ];

    private static readonly string[] OfficeCatalogTerms =
    [
        "escritorio",
        "papelaria",
        "caderno",
        "caneta",
        "papel a4",
        "agrafador",
        "calculadora",
        "arquivo",
        "etiquetas"
    ];

    private static readonly string[] CultureFoodCatalogTerms =
    [
        "livro",
        "livros",
        "manga",
        "romance",
        "vinho",
        "vinhos",
        "gin",
        "whisky",
        "rum",
        "champagne",
        "azeite",
        "cafe em grao",
        "capsulas cafe",
        "delta q",
        "kaffa",
        "dolce gusto",
        "nescafe",
        "kimbo",
        "jogo de tabuleiro",
        "monopoly",
        "catan",
        "dixit",
        "xadrez",
        "hasbro",
        "devir"
    ];

    private static readonly string[] FashionCatalogTerms =
    [
        "moda",
        "sapatilhas",
        "tenis",
        "ténis",
        "trainers",
        "calcado",
        "calçado",
        "roupa",
        "camisola",
        "casaco",
        "mochila",
        "mala",
        "relogio",
        "relógio",
        "new balance",
        "nike",
        "adidas"
    ];

    private static readonly string[] MarketplaceCatalogTerms =
    [
        "mesa",
        "mesas",
        "desk",
        "secretaria",
        "escrivaninha",
        "aspirador",
        "aspiradores",
        "robot aspirador",
        "playstation",
        "ps5",
        "ps4",
        "dualshock",
        "psp",
        "ps vita",
        "nintendo dsi",
        "nintendo",
        "consola",
        "consolas",
        "pro gamer",
        "smartwatch",
        "smartwatch android",
        "relogio inteligente",
        "relógio inteligente",
        "apple watch",
        "galaxy watch",
        "camera vigilancia",
        "câmara vigilância",
        "camara vigilancia",
        "bicicleta",
        "bicicleta eletrica",
        "trotinete",
        "mobilidade"
    ];

    private static readonly (string Canonical, string[] Aliases)[] ExactModelFamilies =
    [
        ("iphone", ["iphone", "ipohone"]),
        ("galaxy", ["galaxy"]),
        ("pixel", ["pixel"]),
        ("macbook", ["macbook"]),
        ("thinkpad", ["thinkpad"]),
        ("xps", ["xps"]),
        ("zenbook", ["zenbook"])
    ];

    public IReadOnlyList<string> Categories => text.IsEnglish
        ? new List<string>
        {
            "Premium smartphones",
            "Chargers and cables",
            "Professional laptops",
            "Energy-efficient appliances",
            "Mice and peripherals under €50",
            "Rugged tablets for factory floor",
            "External storage",
            "Monitors and displays",
            "TV and audio",
            "Coffee machines",
            "Industrial tools",
            "Office chairs",
            "Printers and office",
            "Pet food",
            "Sports equipment",
            "Car accessories",
            "Furniture & decor",
            "Audio & DJ equipment"
        }
        : new List<string>
        {
            "Smartphones premium",
            "Carregadores e cabos",
            "Portáteis profissionais",
            "Eletrodomésticos eficientes",
            "Ratos e periféricos até 50€",
            "Tablets industriais todo-terreno",
            "Armazenamento externo",
            "Monitores e ecrãs",
            "TV e áudio",
            "Máquinas de café",
            "Ferramentas industriais",
            "Cadeiras de escritório",
            "Impressoras e escritório",
            "Alimentação animal",
            "Equipamento desportivo",
            "Acessórios automóvel",
            "Móveis & decoração",
            "Áudio & DJ equipment"
        };

    public IReadOnlyList<CriteriaWeight> DefaultWeights => GetCriteriaWeights(null);

    public IReadOnlyList<ProductResult> Products => GetProducts(null);

    public IReadOnlyList<SellerOffer> SellerOffers => GetSellerOffers(null);

    public IReadOnlyList<FeatureItem> Features => text.IsEnglish
        ? new List<FeatureItem>
        {
            new("01", "Smart prompt", "Describe what you need in natural language, without complex filters.", "#7BE8E0"),
            new("02", "Custom weights", "Choose what matters: price, battery, warranty and support.", "#5EE9A8"),
            new("03", "Independent comparison", "Zero sponsorships, zero affiliates. Products are ranked by merit.", "#B49CFF"),
            new("04", "Anti-fraud verification", "Detect suspicious listings, validate sellers and serial numbers.", "#E97B7B"),
            new("05", "Secure checkout", "Order through the app with authenticity and return guarantees.", "#E9D67B"),
            new("06", "Video tutorials", "Learn each feature in less than 2 minutes in the tutorial center.", "#F0A36A")
        }
        : new List<FeatureItem>
        {
            new("01", "Prompt inteligente", "Descreve em linguagem natural o que procuras — sem filtros complicados.", "#7BE8E0"),
            new("02", "Pesos personalizados", "Define o que valorizas (preço, autonomia, garantia) e a IA ranqueia por ti.", "#5EE9A8"),
            new("03", "Comparação isenta", "Zero patrocínios, zero afiliados. Os produtos são ranqueados pelo mérito.", "#B49CFF"),
            new("04", "Verificação anti-fraude", "Deteção de listagens suspeitas, validação de vendedores e número de série.", "#E97B7B"),
            new("05", "Checkout seguro", "Encomenda directa pela app com garantia de autenticidade e devolução.", "#E9D67B"),
            new("06", "Tutoriais em vídeo", "Aprende cada funcionalidade em menos de 2 minutos no centro de tutoriais.", "#F0A36A")
        };

    public IReadOnlyList<TutorialItem> Tutorials => text.IsEnglish
        ? new List<TutorialItem>
        {
            new("Beginners", "Create the first prompt", "1:42"),
            new("Beginners", "Adjust criteria weights", "2:15"),
            new("Beginners", "Read the AI score", "1:58"),
            new("Advanced", "Add a product manually", "1:30"),
            new("Security", "Detect fake sellers", "3:05"),
            new("Orders", "Order with secure payment", "2:40")
        }
        : new List<TutorialItem>
        {
            new("Iniciantes", "Como criar o primeiro prompt", "1:42"),
            new("Iniciantes", "Ajustar pesos dos critérios", "2:15"),
            new("Iniciantes", "Ler o score da IA", "1:58"),
            new("Avançado", "Adicionar produto manual", "1:30"),
            new("Segurança", "Detectar vendedores falsos", "3:05"),
            new("Encomendas", "Encomendar com pagamento seguro", "2:40")
        };

    private static IReadOnlyList<ProductResult> LaptopProducts { get; } = new List<ProductResult>
    {
        new(
            "macbook-air-m3",
            1,
            94,
            "MacBook Air M3",
            "Apple",
            "1.299 €",
            "#5EE9A8",
            "Recomendado",
            new[] { "13.6\"", "1.24 kg", "18h autonomia", "M3", "16GB", "Garantia 2 anos" },
            new[] { "Autonomia 38% acima da média", "Peso reduzido", "Garantia oficial em Portugal" },
            new[] { "Número de série validado", "Revendedor autorizado", "Stock real confirmado", "Garantia oficial registável em PT", "Sem histórico de contrafação" },
            new[]
            {
                new FraudAlert("marketplace-xyz.com", "Preço 60% abaixo do mercado"),
                new FraudAlert("deals-eu.shop", "Domínio criado há 14 dias"),
                new FraudAlert("tech-outlet.io", "Sem registo fiscal verificável")
            },
            "Este produto cumpre 94% dos critérios definidos. Pontos fortes: autonomia, peso reduzido e garantia oficial."
        ),
        new(
            "macbook-air-m5",
            2,
            92,
            "MacBook Air M5",
            "Apple",
            "1.399 EUR",
            "#7BE8E0",
            "Apple recente",
            new[] { "13\"", "15\"", "M5", "16GB", "512GB SSD", "Garantia 3 anos" },
            new[] { "Autonomia elevada", "Muito leve", "Boa escolha macOS" },
            new[] { "Numero de serie validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa correspondencia para pesquisas MacBook Air M5 recentes."
        ),
        new(
            "macbook-pro-m5",
            3,
            90,
            "MacBook Pro M5 14\"",
            "Apple",
            "1.899 EUR",
            "#B49CFF",
            "Mais performance",
            new[] { "14\"", "M5", "16GB", "1TB SSD", "GPU 10-Core", "Garantia 3 anos" },
            new[] { "Ecra melhor", "Mais performance sustentada", "Boa para trabalho profissional" },
            new[] { "Numero de serie validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Referencia adequada para pedidos MacBook Pro M5."
        ),
        new(
            "macbook-neo-a18",
            4,
            86,
            "MacBook Neo 13\"",
            "Apple",
            "999 EUR",
            "#E9D67B",
            "MacBook compacto",
            new[] { "13\"", "A18 Pro", "8GB", "256GB SSD", "512GB SSD", "Garantia 3 anos" },
            new[] { "Formato compacto", "Boa autonomia", "Entrada no ecossistema Apple" },
            new[] { "Numero de serie validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa correspondencia para pesquisas MacBook Neo com A18 Pro."
        ),
        new(
            "thinkpad-x1-carbon",
            5,
            89,
            "ThinkPad X1 Carbon",
            "Lenovo",
            "1.450 €",
            "#7BE8E0",
            "Melhor garantia",
            new[] { "14\"", "1.12 kg", "15h autonomia", "i7", "16GB", "Garantia 3 anos" },
            new[] { "Mais leve da lista", "Garantia superior", "Bom equilíbrio profissional" },
            new[] { "Revendedor autorizado", "Garantia validada", "Stock cruzado em 3 fontes" },
            Array.Empty<FraudAlert>(),
            "Excelente opção para suporte e mobilidade. Fica atrás do primeiro lugar pelo preço mais alto."
        ),
        new(
            "dell-xps-14",
            6,
            82,
            "Dell XPS 14",
            "Dell",
            "1.380 €",
            "#7BE8E0",
            "Ecrã premium",
            new[] { "14\"", "1.59 kg", "12h autonomia", "i7", "16GB", "Garantia 2 anos" },
            new[] { "Boa performance", "Ecrã forte", "Construção sólida" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha se o ecrã for prioritário. Penalizado pelo peso e autonomia abaixo dos líderes."
        ),
        new(
            "asus-zenbook-14",
            7,
            78,
            "ASUS ZenBook 14",
            "ASUS",
            "1.150 €",
            "#7BE8E0",
            "Melhor preço",
            new[] { "14\"", "1.20 kg", "11h autonomia", "i5", "16GB", "Garantia 2 anos" },
            new[] { "Preço competitivo", "Leve", "Boa relação valor" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "A melhor opção para orçamento controlado. Menos indicado para workloads pesados."
        ),
        new(
            "lenovo-legion-pro-5",
            8,
            86,
            "Lenovo Legion Pro 5",
            "Lenovo",
            "1.699 EUR",
            "#B49CFF",
            "Gaming performance",
            new[] { "16\"", "Ryzen 9", "RTX 5070", "32GB", "1TB SSD", "144 Hz" },
            new[] { "Boa performance gaming", "Ecra rapido", "GPU dedicada" },
            new[] { "Revendedor autorizado", "Garantia validada", "Confirmar configuracao exata" },
            Array.Empty<FraudAlert>(),
            "Boa correspondencia para portateis gaming Lenovo Legion ou HP Omen com GPU dedicada."
        )
    };

    private static IReadOnlyList<ProductResult> SmartphoneProducts { get; } = new List<ProductResult>
    {
        new(
            "iphone-17",
            1,
            96,
            "iPhone 17",
            "Apple",
            "989 \u20AC",
            "#5EE9A8",
            "Recomendado",
            new[] { "6.3\"", "177 g", "30h video", "A19", "256GB", "Garantia 3 anos" },
            new[] { "Ecra Super Retina XDR 120Hz", "Camara Dual Fusion 48MP", "Suporte Apple prolongado" },
            new[] { "Pagina oficial Apple validavel", "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada", "Sem alertas criticos" },
            new[]
            {
                new FraudAlert("marketplace-phone-deals.shop", "Preco muito abaixo do mercado para modelo recente"),
                new FraudAlert("iphone-outlet-eu.net", "Sem informacao fiscal verificavel")
            },
            "Melhor correspondencia para quem procura especificamente o iPhone 17, com ecra 6.3\", chip A19 e camaras 48MP."
        ),
        new(
            "iphone-17-pro",
            2,
            95,
            "iPhone 17 Pro",
            "Apple",
            "1.249 \u20AC",
            "#7BE8E0",
            "Melhor camara",
            new[] { "6.3\"", "206 g", "33h video", "A19 Pro", "256GB", "Garantia 3 anos" },
            new[] { "Sistema Pro Fusion 48MP", "USB-C 10Gb/s", "Boa opcao para fotografia e video" },
            new[] { "Pagina oficial Apple validavel", "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Opcao mais forte da familia iPhone 17 para quem valoriza camara, desempenho e video profissional."
        ),
        new(
            "iphone-15-pro",
            3,
            93,
            "iPhone 15 Pro",
            "Apple",
            "999 €",
            "#5EE9A8",
            "Recomendado",
            new[] { "6.1\"", "187 g", "23h vídeo", "A17 Pro", "128GB", "Garantia 3 anos" },
            new[] { "Melhor ecossistema", "Câmara consistente", "Suporte longo" },
            new[] { "IMEI validável", "Revendedor autorizado", "Garantia UE confirmada", "Stock real confirmado", "Sem alertas críticos" },
            new[]
            {
                new FraudAlert("phones-discount.shop", "Preço 45% abaixo do mercado"),
                new FraudAlert("market-eu-mobile.net", "Sem informação fiscal verificável")
            },
            "Melhor escolha premium para quem valoriza câmara, desempenho e suporte prolongado."
        ),
        new(
            "iphone-16",
            4,
            90,
            "iPhone 16",
            "Apple",
            "779,99 EUR",
            "#7BE8E0",
            "Apple equilibrado",
            new[] { "6.1\"", "A18", "128GB", "256GB", "USB-C", "Garantia 3 anos" },
            new[] { "Boa longevidade", "Ecossistema Apple", "Preco abaixo da gama Pro" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa correspondencia para pesquisas iPhone 16."
        ),
        new(
            "iphone-16e",
            5,
            86,
            "iPhone 16e",
            "Apple",
            "599,99 EUR",
            "#B49CFF",
            "iPhone acessivel",
            new[] { "6.1\"", "A18", "128GB", "USB-C", "Face ID", "Garantia 3 anos" },
            new[] { "Entrada mais barata", "Suporte Apple", "Bom para uso diario" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Referencia adequada para pesquisas iPhone 16e."
        ),
        new(
            "samsung-galaxy-s24",
            6,
            90,
            "Samsung Galaxy S24",
            "Samsung",
            "759 €",
            "#7BE8E0",
            "Melhor Android",
            new[] { "6.2\"", "168 g", "4000 mAh", "Exynos 2400", "256GB", "Garantia 3 anos" },
            new[] { "Ecrã excelente", "Atualizações longas", "Boa autonomia" },
            new[] { "IMEI validável", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "A opção Android mais equilibrada para desempenho, ecrã e longevidade."
        ),
        new(
            "google-pixel-8",
            5,
            86,
            "Google Pixel 8",
            "Google",
            "619 €",
            "#B49CFF",
            "Melhor câmara IA",
            new[] { "6.2\"", "187 g", "4575 mAh", "Tensor G3", "128GB", "Garantia 3 anos" },
            new[] { "Fotografia forte", "Android limpo", "Excelente valor" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Ideal para fotografia computacional e experiência Android limpa."
        ),
        new(
            "xiaomi-14",
            6,
            82,
            "Xiaomi 14",
            "Xiaomi",
            "699 €",
            "#E9D67B",
            "Melhor preço/performance",
            new[] { "6.36\"", "193 g", "4610 mAh", "Snapdragon 8 Gen 3", "256GB", "Garantia 3 anos" },
            new[] { "Performance elevada", "Carregamento rápido", "Preço agressivo" },
            new[] { "Revendedor autorizado", "Garantia UE confirmada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Muito forte em performance e carregamento, com preço competitivo."
        ),
        new(
            "samsung-galaxy-a16",
            7,
            78,
            "Samsung Galaxy A16",
            "Samsung",
            "159 EUR",
            "#7BE8E0",
            "Android acessivel",
            new[] { "6.7\"", "Dual SIM", "128GB", "4GB RAM", "5000 mAh", "Garantia 3 anos" },
            new[] { "Preco baixo", "Ecra grande", "Boa bateria" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa referencia para pedidos Samsung Galaxy A de entrada."
        ),
        new(
            "samsung-galaxy-a56",
            8,
            84,
            "Samsung Galaxy A56 5G",
            "Samsung",
            "379 EUR",
            "#B49CFF",
            "Intermedio Samsung",
            new[] { "6.7\"", "5G", "256GB", "8GB RAM", "5000 mAh", "Garantia 3 anos" },
            new[] { "Boa autonomia", "Atualizacoes longas", "Preco equilibrado" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa correspondencia para quem procura Samsung Galaxy A recente."
        ),
        new(
            "samsung-galaxy-s26-ultra",
            9,
            91,
            "Samsung Galaxy S26 Ultra",
            "Samsung",
            "1.249 EUR",
            "#5EE9A8",
            "Topo Android",
            new[] { "6.9\"", "5G", "256GB", "12GB RAM", "S Pen", "Garantia 3 anos" },
            new[] { "Ecra grande", "Camara premium", "Produtividade com S Pen" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Referencia adequada para pedidos Galaxy S Ultra recentes."
        ),
        new(
            "xiaomi-redmi-note-15-pro",
            10,
            82,
            "Xiaomi Redmi Note 15 Pro 5G",
            "Xiaomi",
            "329 EUR",
            "#E9D67B",
            "Valor Xiaomi",
            new[] { "6.83\"", "5G", "256GB", "8GB RAM", "Bateria grande", "Garantia 3 anos" },
            new[] { "Preco competitivo", "Ecra grande", "Boa autonomia" },
            new[] { "IMEI validavel", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "Boa referencia para pedidos Redmi Note Pro recentes."
        ),
        new(
            "spc-senior-harmony",
            11,
            72,
            "SPC Senior Harmony",
            "SPC",
            "39,99 EUR",
            "#B49CFF",
            "Telemovel senior",
            new[] { "2.4\"", "4G", "Teclas grandes", "SOS", "Base de carga", "Garantia 3 anos" },
            new[] { "Simples de usar", "Preco baixo", "Focado em chamadas" },
            new[] { "IMEI validavel", "Garantia UE confirmada", "Confirmar rede 4G" },
            Array.Empty<FraudAlert>(),
            "Boa referencia para pedidos de telemoveis senior SPC ou Maxcom."
        )
    };

    private static IReadOnlyList<ProductResult> ChargerProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "apple-usb-c-20w-power-adapter",
            1,
            94,
            "Apple Carregador USB-C 20W",
            "Apple",
            "19,99 EUR",
            "#5EE9A8",
            "Oficial iPhone",
            ["Carregador USB-C", "20W", "Power Delivery", "iPhone", "iPad", "Garantia 2 anos"],
            ["Acessorio oficial Apple", "Boa compatibilidade iPhone", "Carregamento rapido"],
            "Escolha segura para quem procura um carregador de iPhone simples, oficial e facil de validar."),
        CatalogProduct(
            "anker-nano-usb-c-30w",
            2,
            92,
            "Anker Nano USB-C 30W",
            "Anker",
            "24,99 EUR",
            "#7BE8E0",
            "Mais compacto",
            ["Carregador USB-C", "30W", "GaN", "Power Delivery", "iPhone", "Garantia 2 anos"],
            ["Formato pequeno", "Boa potencia para iPhone", "Marca conhecida"],
            "Boa alternativa compacta para carregar iPhone e outros equipamentos USB-C."),
        CatalogProduct(
            "ugreen-nexode-usb-c-30w",
            3,
            89,
            "UGREEN Nexode USB-C 30W",
            "UGREEN",
            "16,99 EUR",
            "#B49CFF",
            "Melhor preco",
            ["Carregador USB-C", "30W", "GaN", "Power Delivery", "iPhone", "Garantia 2 anos"],
            ["Preco competitivo", "Carregamento rapido", "Boa compatibilidade"],
            "Opcao economica para quem quer carregador USB-C de iPhone com potencia suficiente."),
        CatalogProduct(
            "belkin-boostcharge-magsafe-15w",
            4,
            86,
            "Belkin BoostCharge MagSafe 15W",
            "Belkin",
            "39,99 EUR",
            "#E9D67B",
            "MagSafe",
            ["Carregador MagSafe", "15W", "Wireless", "iPhone", "Cabo integrado", "Garantia 2 anos"],
            ["Carregamento magnetico", "Marca certificada", "Boa opcao para mesa de cabeceira"],
            "Recomendado quando o pedido privilegia carregamento MagSafe em vez de cabo USB-C.")
    };

    private static IReadOnlyList<ProductResult> ApplianceProducts { get; } = new List<ProductResult>
    {
        new(
            "bosch-serie-6-frigorifico",
            1,
            91,
            "Bosch Serie 6 Frigorífico",
            "Bosch",
            "1.779 €",
            "#5EE9A8",
            "Mais eficiente",
            new[] { "Classe A", "363 L total", "No Frost", "29 dB", "203 cm", "Garantia 3 anos" },
            new[] { "Consumo muito baixo", "Silencioso", "Boa capacidade familiar" },
            new[] { "Número de série validado", "Revendedor autorizado", "Etiqueta energética confirmada", "Stock real confirmado", "Garantia UE confirmada" },
            new[]
            {
                new FraudAlert("home-clearance.shop", "Preço 50% abaixo do mercado"),
                new FraudAlert("outlet-domestico.net", "Sem registo fiscal verificável")
            },
            "Melhor escolha para frigorífico eficiente, silencioso e com garantia forte."
        ),
        new(
            "lg-instaview-combinado",
            2,
            88,
            "LG InstaView Combinado",
            "LG",
            "1.049 €",
            "#7BE8E0",
            "Melhor tecnologia",
            new[] { "Classe A++", "635 L", "DoorCooling+", "36 dB", "Wi-Fi", "Garantia 3 anos" },
            new[] { "Capacidade superior", "Arrefecimento rápido", "Funcionalidades smart" },
            new[] { "Revendedor autorizado", "Garantia validada", "Etiqueta energética confirmada" },
            Array.Empty<FraudAlert>(),
            "Excelente para famílias que precisam de grande capacidade e controlo inteligente."
        ),
        new(
            "miele-w1-lavadora",
            3,
            85,
            "Miele W1 Lavadora",
            "Miele",
            "949 €",
            "#B49CFF",
            "Melhor durabilidade",
            new[] { "Classe A+++", "9 kg", "1400 rpm", "47 dB", "TwinDos", "Garantia 3 anos" },
            new[] { "Construção robusta", "Dosagem automática", "Lavagem silenciosa" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preço inicial."
        ),
        new(
            "samsung-bespoke-lava-loica",
            4,
            81,
            "Samsung Bespoke Lava-loiça",
            "Samsung",
            "579 €",
            "#E9D67B",
            "Melhor preço",
            new[] { "Classe A++", "14 serviços", "44 dB", "Auto Door", "Wi-Fi", "Garantia 3 anos" },
            new[] { "Preço competitivo", "Baixo ruído", "Programas inteligentes" },
            new[] { "Vendedor autorizado", "Garantia UE confirmada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Opção equilibrada para cozinha moderna com bom preço e baixo ruído."
        ),
        CatalogProduct(
            "teka-hcb-6370-forno",
            5,
            80,
            "Teka HCB 6370 Forno",
            "Teka",
            "289 EUR",
            "#7BE8E0",
            "Forno encastre",
            ["Forno", "70 L", "Encastre", "Multifuncoes", "Classe A", "Garantia 3 anos"],
            ["Categoria correta", "Marca comum em PT", "Confirmar dimensoes"],
            "Boa referencia quando o pedido e forno de encastre."),
        CatalogProduct(
            "teka-mw-fs20-microondas",
            6,
            78,
            "Teka MW FS20 Microondas 20L",
            "Teka",
            "89 EUR",
            "#B49CFF",
            "Microondas compacto",
            ["Microondas", "20 L", "Livre instalacao", "Potencia media", "Branco", "Garantia 3 anos"],
            ["Preco acessivel", "Facil de comparar", "Confirmar medidas"],
            "Referencia para pedidos de microondas compacto."),
        CatalogProduct(
            "xiaomi-mi-smart-scale-s400",
            7,
            76,
            "Xiaomi Mi Smart Scale S400",
            "Xiaomi",
            "29,99 EUR",
            "#E9D67B",
            "Balanca inteligente",
            ["Balanca", "Bluetooth", "App", "Metricas corporais", "Casa", "Garantia loja"],
            ["Preco baixo", "Liga a app", "Nao e smartphone"],
            "Boa opcao quando o pedido e balanca inteligente Xiaomi, sem confundir com telemoveis.")
    };

    private static IReadOnlyList<ProductResult> MouseProducts { get; } = new List<ProductResult>
    {
        new(
            "logitech-m650-signature",
            1,
            92,
            "Logitech Signature M650",
            "Logitech",
            "34,99 €",
            "#5EE9A8",
            "Melhor geral",
            new[] { "Rato sem fios", "Bluetooth + USB", "24 meses bateria", "101 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Confortável para uso diário", "Cliques silenciosos", "Boa autonomia" },
            new[] { "Vendedor autorizado", "Garantia validada", "Stock real confirmado", "Preço dentro do mercado", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Melhor escolha abaixo de 50€ para trabalho diário, conforto e autonomia sem fios."
        ),
        new(
            "logitech-g305-lightspeed",
            2,
            90,
            "Logitech G305 Lightspeed",
            "Logitech",
            "44,99 €",
            "#7BE8E0",
            "Melhor gaming",
            new[] { "Rato gaming", "Sem fios", "12000 DPI", "99 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Baixa latência", "Sensor preciso", "Bom para jogos e trabalho" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "A melhor opção para quem quer um rato gaming sem fios até 50€."
        ),
        new(
            "microsoft-bluetooth-mouse",
            3,
            84,
            "Microsoft Bluetooth Mouse",
            "Microsoft",
            "24,99 €",
            "#B49CFF",
            "Mais barato",
            new[] { "Rato bluetooth", "Sem dongle", "12 meses bateria", "78 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Preço baixo", "Simples de transportar", "Bom para portátil" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Opção económica e simples para quem quer um rato bluetooth leve."
        ),
        new(
            "razer-deathadder-essential",
            4,
            82,
            "Razer DeathAdder Essential",
            "Razer",
            "29,99 €",
            "#E9D67B",
            "Melhor ergonómico",
            new[] { "Rato com fio", "6400 DPI", "Ergonómico", "96 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Formato confortável", "Sensor fiável", "Bom preço" },
            new[] { "Vendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha se preferes rato com fio, formato ergonómico e preço baixo."
        )
    };

    private static IReadOnlyList<ProductResult> RuggedTabletProducts { get; } = new List<ProductResult>
    {
        new(
            "getac-ux10-g3",
            1,
            94,
            "Getac UX10 G3",
            "Getac",
            "1.899 €",
            "#5EE9A8",
            "Melhor para chão de fábrica",
            new[] { "10.1\" FHD", "1000 nits", "Windows 11 Pro", "IP66", "MIL-STD-810H", "Toque com luvas" },
            new[] { "Ecrã muito brilhante", "Totalmente rugged", "Boa opção para software Windows" },
            new[] { "Página oficial da marca validável", "Especificações industriais confirmadas", "Vendedor empresarial recomendado" },
            Array.Empty<FraudAlert>(),
            "A escolha mais equilibrada para chão de fábrica quando precisas de tablet touch robusto, boa resolução e compatibilidade com software Windows."
        ),
        new(
            "panasonic-toughbook-g2",
            2,
            91,
            "Panasonic TOUGHBOOK G2",
            "Panasonic",
            "2.999 €",
            "#7BE8E0",
            "Mais modular",
            new[] { "10.1\" WUXGA", "1000 nits", "Windows 11 Pro", "IP65", "MIL-STD-810H", "xPAK modular" },
            new[] { "Muito configurável", "Excelente para equipas de campo", "Boa durabilidade" },
            new[] { "Página oficial da marca validável", "Linha TOUGHBOOK empresarial", "Configuração final depende do distribuidor" },
            Array.Empty<FraudAlert>(),
            "Muito forte para ambientes agressivos e equipas que precisam de módulos como leitor de código de barras, teclado destacável ou LTE."
        ),
        new(
            "zebra-et45-10",
            3,
            87,
            "Zebra ET45 10\"",
            "Zebra",
            "899 €",
            "#B49CFF",
            "Melhor Android empresarial",
            new[] { "10.1\" FHD", "Android Enterprise", "Wi-Fi 6", "5G opcional", "Rugged frame", "Até 10h bateria" },
            new[] { "Gestão empresarial forte", "Boa para logística", "Ecossistema Zebra" },
            new[] { "Página oficial da marca validável", "Especificações empresariais confirmadas", "Preço final depende da configuração" },
            Array.Empty<FraudAlert>(),
            "Boa opção se o software correr em Android ou web app e precisares de gestão empresarial, acessórios e suporte Zebra."
        ),
        new(
            "samsung-galaxy-tab-active4-pro",
            4,
            84,
            "Samsung Galaxy Tab Active4 Pro",
            "Samsung",
            "679 €",
            "#E9D67B",
            "Mais acessível",
            new[] { "10.1\" WUXGA", "Android", "IP68", "MIL-STD-810H", "S Pen IP68", "Bateria removível" },
            new[] { "Bom preço", "Caneta rugged incluída", "Boa opção Android" },
            new[] { "Página oficial da marca validável", "Certificações rugged confirmadas", "Validar versão 5G/Wi-Fi antes da compra" },
            Array.Empty<FraudAlert>(),
            "Alternativa mais acessível para equipas que podem usar Android, web apps ou apps móveis em vez de software Windows nativo."
        )
    };

    private static ProductResult CatalogProduct(
        string slug,
        int rank,
        int score,
        string name,
        string brand,
        string price,
        string accent,
        string badge,
        string[] specs,
        string[] highlights,
        string summary)
    {
        return new ProductResult(
            slug,
            rank,
            score,
            name,
            brand,
            price,
            accent,
            badge,
            specs,
            highlights,
            new[] { "Produto real em retalhistas portugueses", "Marca e categoria validadas", "Preço final e stock a confirmar", "Garantia do vendedor a validar" },
            Array.Empty<FraudAlert>(),
            summary);
    }

    private static IReadOnlyList<ProductResult> StorageProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "western-digital-my-passport-1tb",
            1,
            88,
            "Western Digital My Passport 1TB",
            "Western Digital",
            "58,80 €",
            "#5EE9A8",
            "Melhor Escolha",
            ["1TB", "USB 3.2", "HDD portátil", "2.5\"", "Backup automático", "Garantia 3 anos"],
            ["Preço competitivo", "Formato compacto", "Boa compatibilidade Windows/Mac"],
            "Escolha segura para quem procura um disco externo simples, portátil e barato."),
        CatalogProduct(
            "seagate-expansion-portable-2tb",
            2,
            84,
            "Seagate Expansion Portable 2TB",
            "Seagate",
            "69,99 €",
            "#7BE8E0",
            "Mais capacidade",
            ["2TB", "USB 3.0", "HDD portátil", "Plug and play", "2.5\"", "Garantia 3 anos"],
            ["Mais espaço pelo preço", "Instalação simples", "Boa opção para backups"],
            "Boa opção se o critério principal for capacidade por euro."),
        CatalogProduct(
            "samsung-t7-shield-1tb",
            3,
            82,
            "Samsung T7 Shield 1TB",
            "Samsung",
            "109,90 €",
            "#B49CFF",
            "SSD resistente",
            ["1TB SSD", "USB 3.2 Gen 2", "Até 1050 MB/s", "IP65", "Resistente a quedas", "Garantia 3 anos"],
            ["Muito mais rápido que HDD", "Construção robusta", "Bom para vídeo e trabalho móvel"],
            "Recomendado quando velocidade e resistência pesam mais que preço.")
    };

    private static IReadOnlyList<ProductResult> MonitorProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "lg-ultragear-27gp850-b",
            1,
            90,
            "LG UltraGear 27GP850-B",
            "LG",
            "279 €",
            "#5EE9A8",
            "Melhor QHD gaming",
            ["27\"", "QHD", "Nano IPS", "165 Hz", "1 ms", "G-Sync/FreeSync"],
            ["Boa fluidez", "Painel rápido", "Equilíbrio forte para trabalho e gaming"],
            "Melhor correspondência para quem pede monitor 27 polegadas com boa resolução e alta taxa de atualização."),
        CatalogProduct(
            "samsung-odyssey-g5-27",
            2,
            84,
            "Samsung Odyssey G5 27\"",
            "Samsung",
            "229 €",
            "#7BE8E0",
            "Curvo competitivo",
            ["27\"", "QHD", "VA curvo", "144 Hz", "1 ms", "FreeSync"],
            ["Preço agressivo", "Boa imersão", "Contraste elevado"],
            "Boa escolha para gaming com orçamento controlado."),
        CatalogProduct(
            "dell-p2723d",
            3,
            82,
            "Dell P2723D",
            "Dell",
            "239 €",
            "#B49CFF",
            "Melhor escritório",
            ["27\"", "QHD", "IPS", "75 Hz", "USB-C opcional", "Base ajustável"],
            ["Ergonomia forte", "Boa garantia", "Adequado para produtividade"],
            "Mais indicado para escritório e produtividade do que para jogos competitivos.")
    };

    private static IReadOnlyList<ProductResult> KeyboardProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "logitech-mx-keys-s",
            1,
            88,
            "Logitech MX Keys S",
            "Logitech",
            "109 €",
            "#5EE9A8",
            "Melhor produtividade",
            ["Sem fios", "Bluetooth/Logi Bolt", "Retroiluminado", "Multi-dispositivo", "Layout PT", "USB-C"],
            ["Muito confortável", "Bom para escritório", "Alterna entre vários dispositivos"],
            "Melhor opção para trabalho diário quando se valoriza conforto e baixo ruído."),
        CatalogProduct(
            "keychron-k2-v2",
            2,
            84,
            "Keychron K2 V2",
            "Keychron",
            "89 €",
            "#7BE8E0",
            "Mecânico compacto",
            ["75%", "Bluetooth/USB-C", "Switch mecânico", "Windows/Mac", "Retroiluminado", "Bateria integrada"],
            ["Boa escrita", "Formato compacto", "Compatível com vários sistemas"],
            "Boa escolha para quem quer teclado mecânico sem ocupar muito espaço."),
        CatalogProduct(
            "logitech-k380",
            3,
            80,
            "Logitech K380",
            "Logitech",
            "39,99 €",
            "#B49CFF",
            "Mais portátil",
            ["Bluetooth", "Multi-dispositivo", "Compacto", "Pilhas AAA", "Layout PT", "Até 24 meses"],
            ["Preço baixo", "Fácil de transportar", "Bom para tablets e portáteis"],
            "Opção económica e simples para mobilidade.")
    };

    private static IReadOnlyList<ProductResult> HeadphonesProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "sony-wh-1000xm5",
            1,
            92,
            "Sony WH-1000XM5",
            "Sony",
            "299 €",
            "#5EE9A8",
            "Melhor ANC",
            ["Auscultadores Bluetooth", "Noise cancelling / ANC", "Até 30h", "Multiponto", "USB-C", "Microfones beamforming"],
            ["Cancelamento de ruído forte", "Som equilibrado", "Conforto para viagens"],
            "Melhor escolha quando isolamento e qualidade geral são prioridade."),
        CatalogProduct(
            "apple-airpods-pro-2",
            2,
            89,
            "Apple AirPods Pro 2",
            "Apple",
            "239 €",
            "#7BE8E0",
            "Melhor iPhone",
            ["True wireless", "ANC", "Modo transparência", "USB-C", "Estojo MagSafe", "IP54"],
            ["Integração Apple", "Muito portáteis", "Bom microfone"],
            "A melhor opção para utilizadores iPhone que querem auriculares compactos com ANC."),
        CatalogProduct(
            "jbl-tune-770nc",
            3,
            80,
            "JBL Tune 770NC",
            "JBL",
            "79,99 €",
            "#B49CFF",
            "Bom valor",
            ["Bluetooth", "ANC", "Até 70h", "Multiponto", "USB-C", "Dobráveis"],
            ["Bateria forte", "Preço acessível", "Boa opção diária"],
            "Alternativa económica para quem quer auscultadores Bluetooth com cancelamento de ruído.")
        ,
        CatalogProduct(
            "samsung-galaxy-buds-4-pro",
            4,
            84,
            "Samsung Galaxy Buds 4 Pro",
            "Samsung",
            "199 EUR",
            "#5EE9A8",
            "Auriculares Samsung",
            ["True wireless", "Galaxy Buds", "ANC", "Bluetooth", "Estojo carregamento", "Android"],
            ["Boa integracao Samsung", "Cancelamento de ruido", "Formato compacto"],
            "Boa correspondencia para pedidos de Galaxy Buds ou auriculares Samsung."),
        CatalogProduct(
            "lg-s40t-soundbar",
            5,
            80,
            "LG S40T Soundbar",
            "LG",
            "179 EUR",
            "#E9D67B",
            "Soundbar 2.1",
            ["Soundbar", "2.1 canais", "Subwoofer sem fios", "Bluetooth", "HDMI ARC", "TV audio"],
            ["Melhora som da TV", "Instalacao simples", "Boa opcao sala"],
            "Boa referencia para pedidos de soundbar ou barras de som.")
    };

    private static IReadOnlyList<ProductResult> TvProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "lg-oled-c4-55",
            1,
            92,
            "LG OLED C4 55\"",
            "LG",
            "1.199 €",
            "#5EE9A8",
            "Melhor OLED",
            ["55\"", "OLED evo", "4K", "144 Hz", "HDMI 2.1", "webOS"],
            ["Contraste excelente", "Ótima para cinema e gaming", "HDMI 2.1 completo"],
            "Melhor correspondência para quem pede TV 55 polegadas premium."),
        CatalogProduct(
            "samsung-qn90d-55",
            2,
            88,
            "Samsung QN90D 55\"",
            "Samsung",
            "1.099 €",
            "#7BE8E0",
            "Melhor brilho",
            ["55\"", "Neo QLED", "Mini LED", "4K", "144 Hz", "Tizen"],
            ["Muito brilho", "Boa para salas iluminadas", "Forte em gaming"],
            "Boa escolha para salas com muita luz e uso misto."),
        CatalogProduct(
            "tcl-55c805",
            3,
            82,
            "TCL 55C805",
            "TCL",
            "599 €",
            "#B49CFF",
            "Preço forte",
            ["55\"", "Mini LED", "4K", "144 Hz", "Google TV", "HDMI 2.1"],
            ["Preço competitivo", "Boa ficha técnica", "Google TV integrado"],
            "Opção de valor para quem quer boa imagem sem chegar ao preço das gamas premium."),
        CatalogProduct(
            "tooq-suporte-tv-vesa",
            4,
            76,
            "TooQ Suporte TV VESA",
            "TooQ",
            "24,99 EUR",
            "#E9D67B",
            "Suporte TV",
            ["Suporte TV", "VESA", "Parede", "32 a 65 polegadas", "Carga maxima", "Instalacao"],
            ["Preco baixo", "Confirmar VESA", "Confirmar peso suportado"],
            "Referencia adequada para pedidos de suporte de TV; confirmar VESA, peso e dimensoes.")
    };

    private static IReadOnlyList<ProductResult> CoffeeMachineProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "delonghi-magnifica-start",
            1,
            88,
            "De'Longhi Magnifica Start",
            "De'Longhi",
            "329 €",
            "#5EE9A8",
            "Automática equilibrada",
            ["Máquina de café automática", "Café em grão", "15 bar", "Moinho integrado", "Depósito 1.8 L", "Limpeza automática"],
            ["Bom valor", "Café fresco em grão", "Funcionamento automático"],
            "Boa escolha para quem quer máquina automática sem entrar nos modelos mais caros."),
        CatalogProduct(
            "sage-bambino-plus",
            2,
            86,
            "Sage Bambino Plus",
            "Sage",
            "499 €",
            "#7BE8E0",
            "Melhor espresso manual",
            ["Porta-filtro", "54 mm", "Aquecimento rápido", "Vapor automático", "Compacta", "Inox"],
            ["Espresso forte", "Leite bem texturizado", "Ocupa pouco espaço"],
            "Indicada para quem quer controlar melhor o espresso e bebidas com leite."),
        CatalogProduct(
            "nespresso-vertuo-pop",
            3,
            76,
            "Nespresso Vertuo Pop",
            "Nespresso",
            "69,99 €",
            "#B49CFF",
            "Mais simples",
            ["Cápsulas Vertuo", "Compacta", "Bluetooth/Wi-Fi", "Várias chávenas", "Depósito 0.6 L", "Automática"],
            ["Entrada barata", "Muito fácil de usar", "Boa para uso ocasional"],
            "Opção simples se a prioridade for conveniência e baixo custo inicial.")
    };

    private static IReadOnlyList<ProductResult> CameraProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "canon-eos-r100-kit",
            1,
            84,
            "Canon EOS R100 + RF-S 18-45mm",
            "Canon",
            "549 EUR",
            "#5EE9A8",
            "Mirrorless entrada",
            ["APS-C", "RF-S 18-45mm", "4K", "Visor eletronico", "Wi-Fi", "Garantia loja"],
            ["Boa para iniciar", "Sistema Canon RF", "Kit completo"],
            "Boa correspondencia para pedidos de maquina fotografica Canon."),
        CatalogProduct(
            "fujifilm-instax-mini-12",
            2,
            78,
            "Fujifilm Instax Mini 12",
            "Fujifilm",
            "89,99 EUR",
            "#7BE8E0",
            "Instantanea",
            ["Fotografia instantanea", "Filme Instax Mini", "Automatica", "Compacta", "Uso casual", "Garantia loja"],
            ["Muito simples", "Boa para oferta", "Preco acessivel"],
            "Referencia adequada para maquinas fotograficas instantaneas."),
        CatalogProduct(
            "sony-alpha-6100-kit",
            3,
            82,
            "Sony Alpha 6100 + 16-50mm",
            "Sony",
            "699 EUR",
            "#B49CFF",
            "Autofocus forte",
            ["APS-C", "16-50mm", "4K", "Autofocus rapido", "E-mount", "Garantia loja"],
            ["Boa qualidade imagem", "AF rapido", "Ecossistema Sony"],
            "Boa opcao para pedidos Sony Alpha ou camera fotografica."),
        CatalogProduct(
            "gopro-hero-13",
            4,
            80,
            "GoPro HERO13 Black",
            "GoPro",
            "399 EUR",
            "#E9D67B",
            "Acao 4K",
            ["Camara acao", "4K", "Estabilizacao", "Impermeavel", "Ecras duplos", "Garantia loja"],
            ["Boa para desporto", "Resistente", "Video estabilizado"],
            "Boa correspondencia para pedidos de camera de acao.")
    };

    private static IReadOnlyList<ProductResult> DroneProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "dji-mini-4k",
            1,
            84,
            "DJI Mini 4K",
            "DJI",
            "299 EUR",
            "#5EE9A8",
            "Drone 4K leve",
            ["4K", "Ate 31 min", "Leve", "GPS", "Estabilizacao", "Garantia loja"],
            ["Boa entrada DJI", "Video 4K", "Facil de transportar"],
            "Boa correspondencia para pedidos de drone DJI Mini."),
        CatalogProduct(
            "dji-neo",
            2,
            82,
            "DJI Neo",
            "DJI",
            "199 EUR",
            "#7BE8E0",
            "Mini drone",
            ["4K", "Ate 18 min", "Compacto", "Modos automaticos", "Protecao helices", "Garantia loja"],
            ["Muito compacto", "Facil para iniciantes", "Preco baixo"],
            "Referencia adequada para mini drones DJI Neo."),
        CatalogProduct(
            "dji-mini-3",
            3,
            86,
            "DJI Mini 3",
            "DJI",
            "499 EUR",
            "#B49CFF",
            "Autonomia forte",
            ["4K", "Ate 38 min", "GPS", "DJI RC opcional", "Gimbal 3 eixos", "Garantia loja"],
            ["Boa autonomia", "Imagem estavel", "Ecossistema DJI"],
            "Boa opcao para drones DJI Mini com autonomia superior."),
        CatalogProduct(
            "prixton-delta-drone",
            4,
            72,
            "Prixton Delta Drone",
            "Prixton",
            "49,99 EUR",
            "#E9D67B",
            "Drone entrada",
            ["480p", "Ate 10 min", "Compacto", "Iniciantes", "Cinzento", "Garantia loja"],
            ["Muito acessivel", "Uso ocasional", "Confirmar alcance e bateria"],
            "Referencia adequada para drones Prixton de entrada.")
    };

    private static IReadOnlyList<ProductResult> ToolProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "bosch-professional-gsb-18v-55",
            1,
            88,
            "Bosch Professional GSB 18V-55",
            "Bosch",
            "169 €",
            "#5EE9A8",
            "Melhor 18V",
            ["18V", "Brushless", "55 Nm", "Percussão", "Mandril metálico", "Sistema Professional"],
            ["Boa robustez", "Ecossistema Bosch Pro", "Forte para obra e manutenção"],
            "Escolha equilibrada para bricolage exigente e uso profissional ligeiro."),
        CatalogProduct(
            "makita-dhp482z",
            2,
            84,
            "Makita DHP482Z",
            "Makita",
            "89 €",
            "#7BE8E0",
            "Corpo económico",
            ["18V", "62 Nm", "Percussão", "2 velocidades", "Corpo sem bateria", "LXT"],
            ["Bom preço se já tens baterias", "Robusta", "Ecossistema LXT"],
            "Boa opção para quem já usa baterias Makita 18V."),
        CatalogProduct(
            "dewalt-dcd796p2",
            3,
            82,
            "DeWalt DCD796P2",
            "DeWalt",
            "239 €",
            "#B49CFF",
            "Kit completo",
            ["18V", "Brushless", "70 Nm", "2 baterias", "Mala", "Percussão"],
            ["Kit pronto a usar", "Binário elevado", "Boa para manutenção pesada"],
            "Mais caro, mas interessante quando precisas de kit completo.")
    };

    private static IReadOnlyList<ProductResult> ChairProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "ikea-markus",
            1,
            84,
            "IKEA MARKUS",
            "IKEA",
            "179 €",
            "#5EE9A8",
            "Melhor valor",
            ["Cadeira escritório", "Apoio lombar", "Encosto alto", "Rede", "Altura ajustável", "Garantia longa"],
            ["Boa relação preço/conforto", "Fácil de encontrar", "Adequada para uso diário"],
            "Boa escolha de valor para escritório doméstico ou posto de trabalho simples."),
        CatalogProduct(
            "sihoo-m57",
            2,
            82,
            "SIHOO M57",
            "SIHOO",
            "199 €",
            "#7BE8E0",
            "Mais ajustável",
            ["Cadeira ergonómica", "Apoio lombar", "Encosto em rede", "Braços ajustáveis", "Reclinação", "Apoio de cabeça"],
            ["Mais ajustes", "Boa ventilação", "Conforto prolongado"],
            "Opção interessante quando os ajustes ergonómicos são prioritários."),
        CatalogProduct(
            "songmics-obg22b",
            3,
            76,
            "SONGMICS OBG22B",
            "SONGMICS",
            "89,99 €",
            "#B49CFF",
            "Mais barata",
            ["Cadeira escritório", "Acolchoada", "Altura ajustável", "Braços fixos", "Rodízios", "Uso leve"],
            ["Preço baixo", "Montagem simples", "Adequada para uso ocasional"],
            "Alternativa económica para uso menos intensivo.")
    };

    private static IReadOnlyList<ProductResult> TyreProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "michelin-primacy-4-plus-205-55-r16",
            1,
            88,
            "Michelin Primacy 4+ 205/55 R16",
            "Michelin",
            "92 €",
            "#5EE9A8",
            "Melhor segurança",
            ["205/55 R16", "Verão", "Travagem em molhado", "Etiqueta UE", "Turismo", "Garantia fabricante"],
            ["Boa durabilidade", "Travagem forte", "Marca premium"],
            "Opção segura para quem valoriza aderência e longevidade."),
        CatalogProduct(
            "continental-premiumcontact-7-205-55-r16",
            2,
            86,
            "Continental PremiumContact 7 205/55 R16",
            "Continental",
            "88 €",
            "#7BE8E0",
            "Muito equilibrado",
            ["205/55 R16", "Verão", "Boa aderência", "Etiqueta UE", "Turismo", "Marca premium"],
            ["Boa condução", "Forte em piso molhado", "Preço competitivo"],
            "Escolha equilibrada para condução diária."),
        CatalogProduct(
            "bridgestone-turanza-t005-205-55-r16",
            3,
            80,
            "Bridgestone Turanza T005 205/55 R16",
            "Bridgestone",
            "79 €",
            "#B49CFF",
            "Bom preço premium",
            ["205/55 R16", "Verão", "Turismo", "Baixa resistência", "Etiqueta UE", "Marca premium"],
            ["Preço mais baixo", "Confortável", "Boa eficiência"],
            "Boa alternativa premium quando o orçamento pesa.")
    };

    private static IReadOnlyList<ProductResult> PrinterProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "hp-officejet-pro-9120e",
            1,
            84,
            "HP OfficeJet Pro 9120e",
            "HP",
            "149 €",
            "#5EE9A8",
            "Escritório compacto",
            ["Multifunções", "Jato de tinta", "Wi-Fi", "ADF", "Duplex", "HP Instant Ink"],
            ["Boa para escritório", "Digitalização fácil", "Custo inicial moderado"],
            "Boa opção para pequenos escritórios com impressão e digitalização regulares."),
        CatalogProduct(
            "epson-ecotank-l3250",
            2,
            86,
            "Epson EcoTank L3250",
            "Epson",
            "179 €",
            "#7BE8E0",
            "Baixo custo por página",
            ["Multifunções", "Tanques de tinta", "Wi-Fi", "A4", "Sem tinteiros", "Apps móveis"],
            ["Custo por página baixo", "Boa para volume", "Depósitos recarregáveis"],
            "Melhor se imprimes muitas páginas e queres poupar em tinta."),
        CatalogProduct(
            "brother-dcp-l2620dw",
            3,
            82,
            "Brother DCP-L2620DW",
            "Brother",
            "169 €",
            "#B49CFF",
            "Laser mono",
            ["Laser", "Monocromática", "Wi-Fi", "Duplex", "A4", "Multifunções"],
            ["Texto rápido", "Boa para escritório", "Toner mais previsível"],
            "Indicada para documentos a preto e branco em volume moderado.")
    };

    private static IReadOnlyList<ProductResult> PetFoodProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "royal-canin-medium-adult-15kg",
            1,
            84,
            "Royal Canin Medium Adult 15kg",
            "Royal Canin",
            "62 €",
            "#5EE9A8",
            "Marca premium",
            ["Cão adulto", "15 kg", "Raças médias", "Ração seca", "Nutrição completa", "Saco grande"],
            ["Marca conhecida", "Formato económico", "Boa disponibilidade"],
            "Boa escolha para cães adultos de porte médio quando a prioridade é consistência nutricional."),
        CatalogProduct(
            "purina-pro-plan-medium-adult-14kg",
            2,
            82,
            "Purina Pro Plan Medium Adult 14kg",
            "Purina",
            "54 €",
            "#7BE8E0",
            "Bom equilíbrio",
            ["Cão adulto", "14 kg", "Raças médias", "Ração seca", "Proteína animal", "Saco grande"],
            ["Preço competitivo", "Boa marca", "Fácil de comparar"],
            "Alternativa equilibrada entre preço e qualidade."),
        CatalogProduct(
            "libra-adult-frango-14kg",
            3,
            74,
            "Libra Adult Frango 14kg",
            "Libra",
            "29,99 €",
            "#B49CFF",
            "Mais económica",
            ["Cão adulto", "14 kg", "Frango", "Ração seca", "Saco grande", "Preço baixo"],
            ["Muito barata", "Formato grande", "Adequada a orçamento controlado"],
            "Opção económica quando o preço é o critério principal.")
        ,
        CatalogProduct(
            "frontpro-antiparasitario-caes",
            4,
            80,
            "Frontpro Antiparasitario Caes",
            "Frontpro",
            "24,99 EUR",
            "#7BE8E0",
            "Veterinario",
            ["Caes", "Comprimidos mastigaveis", "Antiparasitario", "Confirmar peso", "Farmacia veterinaria", "Garantia loja"],
            ["Categoria veterinaria", "Confirmar peso do animal", "Marca conhecida"],
            "Boa referencia para pedidos veterinarios de animais, sem confundir com suplementos humanos."),
        CatalogProduct(
            "scalibor-coleira-65cm",
            5,
            78,
            "Scalibor Coleira Antiparasitaria 65cm",
            "Scalibor",
            "19,99 EUR",
            "#B49CFF",
            "Coleira antiparasitaria",
            ["Cao", "Coleira", "65 cm", "Antiparasitario", "Confirmar porte", "Garantia loja"],
            ["Produto conhecido", "Confirmar tamanho", "Categoria animal"],
            "Opcao adequada para pedidos de coleiras antiparasitarias.")
    };

    private static IReadOnlyList<ProductResult> BabyCareProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "pampers-premium-protection-t4",
            1,
            84,
            "Pampers Premium Protection T4",
            "Pampers",
            "24,99 €",
            "#5EE9A8",
            "Fralda premium",
            ["Tamanho 4", "Bebé", "Absorção elevada", "Pele sensível", "Pack familiar", "Descartável"],
            ["Boa absorção", "Marca conhecida", "Fácil de encontrar"],
            "Boa opção para quem procura fraldas premium com boa absorção."),
        CatalogProduct(
            "dodot-aqua-pure",
            2,
            78,
            "Dodot Aqua Pure Toalhitas",
            "Dodot",
            "19,99 €",
            "#7BE8E0",
            "Toalhitas sensíveis",
            ["Toalhitas", "99% água", "Pele sensível", "Pack múltiplo", "Sem perfume", "Bebé"],
            ["Boas para pele sensível", "Pack económico", "Complemento às fraldas"],
            "Produto complementar para higiene diária do bebé."),
        CatalogProduct(
            "chicco-next2me",
            3,
            82,
            "Chicco Next2Me",
            "Chicco",
            "199 €",
            "#B49CFF",
            "Berço lateral",
            ["Berço bebé", "Co-sleeping", "Altura ajustável", "Rebatível", "Transporte", "0-6 meses"],
            ["Marca conhecida", "Prático para recém-nascidos", "Bom para quarto pequeno"],
            "Boa opção quando o pedido é puericultura e não apenas consumíveis.")
    };

    private static IReadOnlyList<ProductResult> TabletProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "apple-ipad-11-a16-128gb",
            1,
            88,
            "Apple iPad 11 A16 128GB Wi-Fi",
            "Apple",
            "349 EUR",
            "#5EE9A8",
            "Tablet equilibrado",
            ["11\"", "A16", "128GB", "Wi-Fi", "iPadOS", "Garantia 3 anos"],
            ["Ecra forte", "Ecossistema Apple", "Boa longevidade"],
            "Escolha segura quando o pedido e um tablet normal para estudo, trabalho leve ou consumo de conteudo."),
        CatalogProduct(
            "samsung-galaxy-tab-a11",
            2,
            82,
            "Samsung Galaxy Tab A11 Wi-Fi",
            "Samsung",
            "149 EUR",
            "#7BE8E0",
            "Android acessivel",
            ["8.7\"", "Android", "64GB", "Wi-Fi", "Compacto", "Garantia 3 anos"],
            ["Preco baixo", "Formato compacto", "Bom para uso casual"],
            "Boa opcao Android quando o orcamento e o criterio principal."),
        CatalogProduct(
            "lenovo-idea-tab-11",
            3,
            80,
            "Lenovo Idea Tab 11",
            "Lenovo",
            "179 EUR",
            "#B49CFF",
            "Bom valor",
            ["11\"", "Android", "128GB", "Wi-Fi", "Caneta opcional", "Garantia 3 anos"],
            ["Bom equilibrio", "Ecra amplo", "Preco competitivo"],
            "Alternativa de valor para aulas, notas e navegacao."),
        CatalogProduct(
            "samsung-galaxy-tab-s10-lite",
            4,
            84,
            "Samsung Galaxy Tab S10 Lite",
            "Samsung",
            "329 EUR",
            "#5EE9A8",
            "Tablet Samsung",
            ["10.9\"", "Android", "128GB", "Wi-Fi", "S Pen opcional", "Garantia 3 anos"],
            ["Boa para notas", "Ecra amplo", "Ecossistema Samsung"],
            "Boa correspondencia para pedidos Galaxy Tab recentes."),
        CatalogProduct(
            "xiaomi-pad-7",
            5,
            82,
            "Xiaomi Pad 7",
            "Xiaomi",
            "299 EUR",
            "#7BE8E0",
            "Tablet Xiaomi",
            ["11.2\"", "Android", "128GB", "Wi-Fi", "Ecra rapido", "Garantia 3 anos"],
            ["Boa ficha tecnica", "Preco competitivo", "Adequado para media"],
            "Referencia para pedidos Xiaomi Pad ou Redmi Pad.")
    };

    private static IReadOnlyList<ProductResult> HealthBeautyProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "tecnifar-artrozen-60",
            1,
            82,
            "Tecnifar Artrozen 60 Comprimidos",
            "Tecnifar",
            "15,61 EUR",
            "#5EE9A8",
            "Suplemento articulacoes",
            ["60 comprimidos", "Articulacoes", "Suplemento", "Adulto", "Confirmar composicao", "Garantia loja"],
            ["Produto comum em farmacia", "Preco facil de comparar", "Confirmar indicacao medica"],
            "Opcao representativa quando o pedido e suplemento de saude; confirmar composicao e aconselhamento profissional antes de comprar."),
        CatalogProduct(
            "la-roche-posay-cicaplast-baume-b5",
            2,
            84,
            "La Roche-Posay Cicaplast Baume B5+",
            "La Roche-Posay",
            "11,99 EUR",
            "#7BE8E0",
            "Creme reparador",
            ["Creme", "Pele sensivel", "100 ml", "Dermocosmetica", "Sem perfume", "Farmacia"],
            ["Marca reconhecida", "Boa disponibilidade", "Uso familiar"],
            "Boa correspondencia para pedidos de creme reparador ou cuidados de pele sensivel."),
        CatalogProduct(
            "dolce-gabbana-light-blue",
            3,
            80,
            "Dolce & Gabbana Light Blue Eau de Toilette",
            "Dolce & Gabbana",
            "54,90 EUR",
            "#B49CFF",
            "Perfume conhecido",
            ["Eau de Toilette", "100 ml", "Perfume", "Feminino", "Confirmar vendedor", "Garantia loja"],
            ["Produto popular", "Comparar capacidade", "Atencao a falsificacoes"],
            "Boa referencia quando o pedido e perfume; validar sempre capacidade e vendedor."),
        CatalogProduct(
            "isdin-fotoprotector-fusion-water",
            4,
            78,
            "ISDIN Fotoprotector Fusion Water SPF50",
            "ISDIN",
            "18,90 EUR",
            "#E9D67B",
            "Protecao solar",
            ["SPF50", "50 ml", "Rosto", "Textura leve", "Dermocosmetica", "Farmacia"],
            ["Produto muito conhecido", "Boa para comparacao de preco", "Confirmar tipo de pele"],
            "Referencia adequada para pedidos de protetor solar facial.")
    };

    private static IReadOnlyList<ProductResult> ToyProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "lego-icons-ferrari-f2004",
            1,
            86,
            "LEGO Icons Ferrari F2004 Michael Schumacher",
            "LEGO",
            "79,99 EUR",
            "#5EE9A8",
            "LEGO colecionavel",
            ["LEGO", "Icons", "Colecionavel", "Idade recomendada", "Confirmar pecas", "Garantia loja"],
            ["Marca oficial", "Boa procura", "Confirmar referencia"],
            "Boa correspondencia para pedidos LEGO ou brinquedos de construcao."),
        CatalogProduct(
            "lego-minecraft-creeper",
            2,
            82,
            "LEGO Minecraft O Creeper",
            "LEGO",
            "39,99 EUR",
            "#7BE8E0",
            "LEGO infantil",
            ["LEGO", "Minecraft", "Construcao", "Idade recomendada", "Set tematico", "Garantia loja"],
            ["Tema popular", "Preco facil de comparar", "Boa oferta para criancas"],
            "Opcao adequada para pedidos de LEGO tematico."),
        CatalogProduct(
            "hot-wheels-pack-tematico",
            3,
            76,
            "Hot Wheels Pack Tematico",
            "Mattel",
            "12,99 EUR",
            "#B49CFF",
            "Brinquedo acessivel",
            ["Carros", "Brinquedo", "Pack", "Colecionavel", "Idade recomendada", "Garantia loja"],
            ["Preco baixo", "Facil de encontrar", "Boa compra ocasional"],
            "Alternativa para pedidos genericos de brinquedos.")
    };

    private static IReadOnlyList<ProductResult> HomeProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "ikea-lagkapten-adils",
            1,
            82,
            "IKEA LAGKAPTEN / ADILS",
            "IKEA",
            "49,99 EUR",
            "#5EE9A8",
            "Mesa simples",
            ["Mesa escritorio", "120x60 cm", "Tampo branco", "Pernas metalicas", "Montagem simples", "Uso leve"],
            ["Preco baixo", "Boa para home office", "Facil de substituir"],
            "Boa opcao quando o pedido e uma mesa de escritorio simples e barata."),
        CatalogProduct(
            "emma-original-colchao",
            2,
            84,
            "Emma Original Colchao",
            "Emma",
            "299 EUR",
            "#7BE8E0",
            "Colchao popular",
            ["Colchao", "Espuma", "Varios tamanhos", "Dormir", "Entrega domicilio", "Garantia loja"],
            ["Marca conhecida", "Comparar tamanho", "Boa disponibilidade online"],
            "Referencia adequada para pedidos de colchao ou artigos de quarto."),
        CatalogProduct(
            "philips-hue-white-color-e27",
            3,
            80,
            "Philips Hue White and Color E27",
            "Philips Hue",
            "39,99 EUR",
            "#B49CFF",
            "Iluminacao smart",
            ["Lampada E27", "RGB", "App", "Zigbee/Bluetooth", "Casa inteligente", "Garantia loja"],
            ["Ecossistema forte", "Boa integracao", "Preco comparavel"],
            "Boa opcao para pedidos de iluminacao e decoracao inteligente.")
    };

    private static IReadOnlyList<ProductResult> SportProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "garmin-forerunner-255",
            1,
            84,
            "Garmin Forerunner 255",
            "Garmin",
            "249 EUR",
            "#5EE9A8",
            "Desporto GPS",
            ["Relogio desportivo", "GPS", "Corrida", "Multidesporto", "Bateria longa", "Garmin Connect"],
            ["Forte para treino", "Boa autonomia", "Metricas avancadas"],
            "Recomendado quando o pedido e equipamento desportivo ou relogio para treino."),
        CatalogProduct(
            "xiaomi-electric-scooter-4",
            2,
            82,
            "Xiaomi Electric Scooter 4",
            "Xiaomi",
            "449 EUR",
            "#7BE8E0",
            "Mobilidade urbana",
            ["Trotinete eletrica", "Ate 25 km/h", "Autonomia urbana", "Dobravel", "App", "Travagem dupla"],
            ["Boa para cidade", "Facil de transportar", "Marca conhecida"],
            "Boa opcao para mobilidade urbana leve."),
        CatalogProduct(
            "domyos-run100-passadeira",
            3,
            78,
            "Domyos RUN100 Passadeira",
            "Domyos",
            "499 EUR",
            "#B49CFF",
            "Fitness casa",
            ["Passadeira", "Dobravel", "Treino indoor", "Velocidade ajustavel", "Uso domestico", "Garantia loja"],
            ["Boa para casa", "Ocupa menos espaco", "Comparar entrega"],
            "Referencia para pedidos de equipamento de fitness domestico."),
        CatalogProduct(
            "asics-gel-kayano-32",
            4,
            82,
            "ASICS Gel-Kayano 32",
            "ASICS",
            "159 EUR",
            "#5EE9A8",
            "Sapatilhas running",
            ["Running", "Estabilidade", "Estrada", "Varios tamanhos", "Confirmar numeracao", "Garantia loja"],
            ["Marca de running", "Boa estabilidade", "Confirmar tamanho"],
            "Boa referencia quando o pedido e sapatilhas de running."),
        CatalogProduct(
            "adidas-adizero-evo-sl",
            5,
            80,
            "Adidas Adizero Evo SL",
            "Adidas",
            "149 EUR",
            "#7BE8E0",
            "Running leve",
            ["Running", "Leve", "Estrada", "Varios tamanhos", "Confirmar numeracao", "Garantia loja"],
            ["Boa para treino rapido", "Marca conhecida", "Comparar cor e tamanho"],
            "Opcao adequada para pedidos de sapatilhas Adidas ou running leve.")
    };

    private static IReadOnlyList<ProductResult> AutoProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "castrol-edge-5w30-5l",
            1,
            80,
            "Castrol EDGE 5W-30 LL 5L",
            "Castrol",
            "42,90 EUR",
            "#5EE9A8",
            "Oleo motor",
            ["5W-30", "5L", "LongLife", "Motor", "Confirmar norma", "Garantia loja"],
            ["Marca conhecida", "Comparar norma do fabricante", "Boa disponibilidade"],
            "Boa referencia quando o pedido e oleo de motor."),
        CatalogProduct(
            "yuasa-ybx-car-battery",
            2,
            78,
            "Yuasa YBX Bateria Auto",
            "Yuasa",
            "89 EUR",
            "#7BE8E0",
            "Bateria auto",
            ["12V", "Bateria auto", "Confirmar amperagem", "Arranque", "Garantia loja", "Compatibilidade veiculo"],
            ["Marca conhecida", "Confirmar dimensoes", "Boa categoria para comparacao"],
            "Referencia para pedidos de bateria automovel."),
        CatalogProduct(
            "bosch-aerotwin-escovas",
            3,
            76,
            "Bosch Aerotwin Escovas Limpa Vidros",
            "Bosch",
            "24,90 EUR",
            "#B49CFF",
            "Acessorio auto",
            ["Escovas", "Limpa vidros", "Automovel", "Confirmar compatibilidade", "Par", "Garantia loja"],
            ["Marca conhecida", "Preco baixo", "Compatibilidade a validar"],
            "Opcao adequada para pedidos de acessorios automovel.")
    };

    private static IReadOnlyList<ProductResult> OfficeProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "navigator-universal-a4-80g",
            1,
            78,
            "Navigator Universal A4 80g",
            "Navigator",
            "4,99 EUR",
            "#5EE9A8",
            "Papel A4",
            ["A4", "80 g", "500 folhas", "Impressao", "Escritorio", "Papelaria"],
            ["Produto comum", "Preco comparavel", "Boa disponibilidade"],
            "Boa referencia para pedidos de papelaria e consumiveis de escritorio."),
        CatalogProduct(
            "bic-cristal-pack",
            2,
            74,
            "BIC Cristal Pack Canetas",
            "BIC",
            "3,49 EUR",
            "#7BE8E0",
            "Canetas",
            ["Escrita", "Pack", "Azul/preto", "Papelaria", "Uso diario", "Preco baixo"],
            ["Marca conhecida", "Facil de encontrar", "Compra simples"],
            "Opcao adequada para material de escrita."),
        CatalogProduct(
            "casio-fx-991cw",
            3,
            82,
            "Casio FX-991CW",
            "Casio",
            "29,99 EUR",
            "#B49CFF",
            "Calculadora cientifica",
            ["Calculadora", "Cientifica", "Escola", "Solar/bateria", "Funcoes avancadas", "Garantia loja"],
            ["Modelo popular", "Boa para estudantes", "Confirmar permissao escolar"],
            "Boa correspondencia para pedidos de calculadora ou material escolar.")
    };

    private static IReadOnlyList<ProductResult> CultureFoodProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "livro-atomic-habits",
            1,
            78,
            "Livro Atomic Habits",
            "Penguin",
            "16,99 EUR",
            "#5EE9A8",
            "Livro popular",
            ["Livro", "Desenvolvimento pessoal", "Capa mole", "Confirmar idioma", "ISBN", "Garantia loja"],
            ["Titulo conhecido", "Comparar edicao", "Boa disponibilidade"],
            "Referencia para pedidos de livros; confirmar sempre idioma e edicao."),
        CatalogProduct(
            "manga-one-piece-volume-1",
            2,
            76,
            "One Piece Volume 1",
            "Panini",
            "8,99 EUR",
            "#7BE8E0",
            "Manga",
            ["Manga", "Volume 1", "Banda desenhada", "Confirmar idioma", "Colecao", "ISBN"],
            ["Boa entrada na colecao", "Preco baixo", "Confirmar edicao"],
            "Boa opcao para pedidos de manga ou cultura pop."),
        CatalogProduct(
            "papa-figos-douro-tinto",
            3,
            82,
            "Papa Figos Douro Tinto",
            "Casa Ferreirinha",
            "6,99 EUR",
            "#B49CFF",
            "Vinho tinto",
            ["Douro", "Tinto", "750 ml", "Portugal", "Confirmar ano", "Venda responsavel"],
            ["Marca conhecida", "Preco comparavel", "Boa disponibilidade"],
            "Referencia para pedidos de vinho tinto portugues."),
        CatalogProduct(
            "bombay-sapphire-gin",
            4,
            78,
            "Bombay Sapphire Gin 70cl",
            "Bombay Sapphire",
            "17,99 EUR",
            "#E9D67B",
            "Gin conhecido",
            ["Gin", "70 cl", "Bebida espirituosa", "Confirmar idade", "Venda responsavel", "Preco comparavel"],
            ["Marca conhecida", "Boa disponibilidade", "Comparar volume"],
            "Opcao conhecida quando o pedido e gin ou bebidas espirituosas."),
        CatalogProduct(
            "delta-q-qalidus-capsulas",
            5,
            78,
            "Delta Q Qalidus Capsulas",
            "Delta Q",
            "15,99 EUR",
            "#5EE9A8",
            "Capsulas cafe",
            ["Capsulas", "Cafe", "Delta Q", "Pack familiar", "Confirmar quantidade", "Garantia loja"],
            ["Categoria correta", "Comparar unidades", "Boa disponibilidade"],
            "Referencia para pedidos de capsulas de cafe Delta Q, Kaffa ou Dolce Gusto."),
        CatalogProduct(
            "catan-jogo-tabuleiro",
            6,
            82,
            "Catan Jogo de Tabuleiro",
            "Devir",
            "34,99 EUR",
            "#7BE8E0",
            "Jogo de tabuleiro",
            ["Jogo de tabuleiro", "Familia", "Estrategia", "Confirmar idioma", "2-4 jogadores", "Garantia loja"],
            ["Titulo conhecido", "Boa disponibilidade", "Confirmar edicao"],
            "Boa correspondencia para pedidos de jogos de tabuleiro como Catan, Monopoly ou Dixit.")
    };

    private static IReadOnlyList<ProductResult> MarketplaceProducts { get; } = new List<ProductResult>
    {
        CatalogProduct(
            "ikea-lagkapten-adils",
            1,
            82,
            "IKEA LAGKAPTEN / ADILS",
            "IKEA",
            "49,99 €",
            "#5EE9A8",
            "Mesa simples",
            ["Mesa escritório", "120x60 cm", "Tampo branco", "Pernas metálicas", "Montagem simples", "Uso leve"],
            ["Preço baixo", "Boa para home office", "Fácil de substituir"],
            "Boa opção quando o pedido é uma mesa de escritório simples e barata."),
        CatalogProduct(
            "ikea-micke-secretaria",
            2,
            80,
            "IKEA MICKE Secretária",
            "IKEA",
            "89,99 €",
            "#7BE8E0",
            "Mais arrumação",
            ["Secretária", "105x50 cm", "Gaveta", "Gestão de cabos", "Home office", "Branco"],
            ["Compacta", "Inclui arrumação", "Boa para quartos pequenos"],
            "Alternativa melhor quando é preciso arrumação integrada."),
        CatalogProduct(
            "roborock-q8-max",
            3,
            88,
            "Roborock Q8 Max",
            "Roborock",
            "349 €",
            "#B49CFF",
            "Aspirador robot",
            ["Robot aspirador", "Mapeamento LiDAR", "Aspira e lava", "App", "Boa sucção", "Base simples"],
            ["Boa navegação", "Automatiza limpeza diária", "Forte relação preço/funções"],
            "Boa escolha para quem procura aspirador robot com mapeamento fiável."),
        CatalogProduct(
            "irobot-roomba-combo-j5",
            4,
            84,
            "iRobot Roomba Combo j5",
            "iRobot",
            "399 €",
            "#E9D67B",
            "Marca conhecida",
            ["Robot aspirador", "Aspira e lava", "Deteção de obstáculos", "App", "Wi-Fi", "Boa assistência"],
            ["Marca reconhecida", "Boa assistência", "Adequado para limpeza regular"],
            "Alternativa sólida para quem privilegia marca e suporte."),
        CatalogProduct(
            "playstation-5-slim",
            5,
            90,
            "PlayStation 5 Slim",
            "Sony",
            "449 €",
            "#5EE9A8",
            "Consola principal",
            ["Consola gaming", "PS5", "1TB", "4K", "DualSense", "Com leitor opcional"],
            ["Grande catálogo", "Boa performance", "Ecossistema PlayStation"],
            "Escolha principal para quem pede consola PlayStation ou PS5."),
        CatalogProduct(
            "nintendo-switch-oled",
            6,
            84,
            "Nintendo Switch OLED",
            "Nintendo",
            "319 €",
            "#7BE8E0",
            "Portátil familiar",
            ["Consola híbrida", "Ecrã OLED", "7\"", "Joy-Con", "Dock", "Jogos família"],
            ["Muito versátil", "Boa para famílias", "Catálogo Nintendo"],
            "Melhor opção se o pedido valoriza portabilidade e jogos familiares."),
        CatalogProduct(
            "apple-watch-series-10",
            7,
            88,
            "Apple Watch Series 10",
            "Apple",
            "459 €",
            "#B49CFF",
            "Smartwatch iPhone",
            ["Smartwatch", "watchOS", "GPS", "Ecrã OLED", "Saúde", "Integração iPhone"],
            ["Melhor integração Apple", "Bom ecossistema de apps", "Métricas de saúde"],
            "Melhor smartwatch para utilizadores iPhone."),
        CatalogProduct(
            "samsung-galaxy-watch7",
            8,
            84,
            "Samsung Galaxy Watch7",
            "Samsung",
            "249 €",
            "#E9D67B",
            "Smartwatch Android",
            ["Smartwatch", "Wear OS", "GPS", "Saúde", "Sono", "Android"],
            ["Boa opção Android", "Preço competitivo", "Métricas completas"],
            "Boa escolha para utilizadores Android."),
        CatalogProduct(
            "samsung-galaxy-watch8",
            9,
            85,
            "Samsung Galaxy Watch8",
            "Samsung",
            "299 EUR",
            "#5EE9A8",
            "Smartwatch Android",
            ["Smartwatch", "Galaxy Watch", "Wear OS", "GPS", "Saude", "Sono"],
            ["Boa opcao Android", "Metricas completas", "Modelo atual a confirmar"],
            "Boa correspondencia para pedidos de Galaxy Watch 8."),
        CatalogProduct(
            "samsung-galaxy-fit3",
            10,
            78,
            "Samsung Galaxy Fit3",
            "Samsung",
            "59,99 EUR",
            "#7BE8E0",
            "Pulseira fitness",
            ["Smartband", "Galaxy Fit", "Monitorizacao saude", "Sono", "Ecrã AMOLED", "Bateria longa"],
            ["Leve", "Boa autonomia", "Preco acessivel"],
            "Boa correspondencia para pedidos de Galaxy Fit 3."),
        CatalogProduct(
            "xiaomi-smart-band-9",
            11,
            78,
            "Xiaomi Smart Band 9",
            "Xiaomi",
            "39,99 EUR",
            "#B49CFF",
            "Smartband",
            ["Smartband", "Pulseira desportiva", "Monitorizacao saude", "Sono", "AMOLED", "Bateria longa"],
            ["Preco baixo", "Boa autonomia", "Forte para notificacoes simples"],
            "Boa correspondencia para pedidos de Xiaomi Smart Band ou pulseiras desportivas."),
        CatalogProduct(
            "tp-link-tapo-c200",
            9,
            78,
            "TP-Link Tapo C200",
            "TP-Link",
            "29,99 €",
            "#5EE9A8",
            "Câmara económica",
            ["Câmara vigilância", "1080p", "Wi-Fi", "Pan/Tilt", "Visão noturna", "App"],
            ["Muito barata", "Instalação simples", "Boa para interior"],
            "Opção acessível para vigilância doméstica interior."),
        CatalogProduct(
            "xiaomi-electric-scooter-4",
            10,
            82,
            "Xiaomi Electric Scooter 4",
            "Xiaomi",
            "449 €",
            "#7BE8E0",
            "Mobilidade urbana",
            ["Trotinete elétrica", "Até 25 km/h", "Autonomia urbana", "Dobrável", "App", "Travagem dupla"],
            ["Boa para cidade", "Fácil de transportar", "Marca conhecida"],
            "Boa opção para mobilidade urbana leve."),
        CatalogProduct(
            "garmin-forerunner-255",
            11,
            84,
            "Garmin Forerunner 255",
            "Garmin",
            "249 €",
            "#B49CFF",
            "Desporto GPS",
            ["Relógio desportivo", "GPS", "Corrida", "Multidesporto", "Bateria longa", "Garmin Connect"],
            ["Forte para treino", "Boa autonomia", "Métricas avançadas"],
            "Recomendado quando o pedido é equipamento desportivo ou relógio para treino.")
    };

    private static IReadOnlyList<ProductResult> AllProducts { get; } = LaptopProducts
        .Concat(SmartphoneProducts)
        .Concat(ChargerProducts)
        .Concat(ApplianceProducts)
        .Concat(MouseProducts)
        .Concat(RuggedTabletProducts)
        .Concat(TabletProducts)
        .Concat(StorageProducts)
        .Concat(MonitorProducts)
        .Concat(KeyboardProducts)
        .Concat(HeadphonesProducts)
        .Concat(TvProducts)
        .Concat(CameraProducts)
        .Concat(DroneProducts)
        .Concat(CoffeeMachineProducts)
        .Concat(ToolProducts)
        .Concat(ChairProducts)
        .Concat(TyreProducts)
        .Concat(PrinterProducts)
        .Concat(PetFoodProducts)
        .Concat(BabyCareProducts)
        .Concat(ToyProducts)
        .Concat(HealthBeautyProducts)
        .Concat(HomeProducts)
        .Concat(SportProducts)
        .Concat(AutoProducts)
        .Concat(OfficeProducts)
        .Concat(CultureFoodProducts)
        .Concat(MarketplaceProducts)
        .ToList();

    private static IReadOnlyList<RetailerProfile> RetailerProfiles { get; } = new List<RetailerProfile>
    {
        new("PT", "Portugal", "Portugal", "Apple Store PT", [LaptopCatalog, SmartphoneCatalog, ChargerCatalog, HeadphonesCatalog], ["Apple"], 98, 1.00m, "2 dias", "2 days", "2 anos oficial", "2-year official", "Verificado", "Verified", "https://www.apple.com/pt/search/{0}", true),
        new("PT", "Portugal", "Portugal", "Samsung Store PT", [SmartphoneCatalog, ApplianceCatalog, StorageCatalog, MonitorCatalog, HeadphonesCatalog, TvCatalog], ["Samsung"], 96, 1.01m, "2 dias", "2 days", "3 anos oficial", "3-year official", "Verificado", "Verified", "https://www.samsung.com/pt/search/?searchvalue={0}", true),
        new("PT", "Portugal", "Portugal", "Worten", [], [], 93, 1.02m, "2 dias", "2 days", "3 anos PT", "3-year PT", "Verificado", "Verified", "https://www.worten.pt/search?query={0}", true),
        new("PT", "Portugal", "Portugal", "FNAC", [LaptopCatalog, SmartphoneCatalog, ChargerCatalog, MouseCatalog, RuggedTabletCatalog, StorageCatalog, MonitorCatalog, KeyboardCatalog, HeadphonesCatalog, TvCatalog, PrinterCatalog, BabyCareCatalog, GeneralCatalog], [], 90, 1.03m, "3 dias", "3 days", "3 anos PT", "3-year PT", "Verificado", "Verified", "https://www.fnac.pt/SearchResult/ResultList.aspx?Search={0}", true),
        new("PT", "Portugal", "Portugal", "MediaMarkt", [], [], 91, 1.01m, "2-3 dias", "2-3 days", "3 anos PT", "3-year PT", "Verificado", "Verified", "https://www.mediamarkt.pt/pt/search.html?query={0}", true),
        new("PT", "Portugal", "Portugal", "PCDIGA", [LaptopCatalog, ChargerCatalog, MouseCatalog, RuggedTabletCatalog, StorageCatalog, MonitorCatalog, KeyboardCatalog, HeadphonesCatalog, PrinterCatalog, GeneralCatalog], [], 92, 0.99m, "1-2 dias", "1-2 days", "3 anos PT", "3-year PT", "Verificado", "Verified", "https://www.pcdiga.com/pesquisa/{0}", true),
        new("PT", "Portugal", "Portugal", "Radio Popular", [ApplianceCatalog, SmartphoneCatalog, ChargerCatalog, MouseCatalog, HeadphonesCatalog, TvCatalog, CoffeeMachineCatalog, GeneralCatalog], [], 89, 1.00m, "3 dias", "3 days", "3 anos PT", "3-year PT", "Verificado", "Verified", "https://www.radiopopular.pt/pesquisa/{0}", true),
        new("PT", "Portugal", "Portugal", "Amazon.es", [], [], 88, 0.98m, "2-5 dias", "2-5 days", "3 anos UE", "3-year EU", "Autorizado", "Authorized", "https://www.amazon.es/s?k={0}", true),
        new("PT", "Portugal", "Portugal", "KuantoKusta", [], [], 82, 0.97m, "Confirmar loja", "Confirm store", "Validar vendedor", "Validate seller", "Comparador", "Comparator", "https://www.kuantokusta.pt/search?q={0}", false),

        new("US", "Estados Unidos", "United States", "Apple Store", [LaptopCatalog, SmartphoneCatalog, ChargerCatalog, HeadphonesCatalog], ["Apple"], 98, 1.00m, "2 days", "2 days", "1-year official", "1-year official", "Verified", "Verified", "https://www.apple.com/us/search/{0}", true),
        new("US", "Estados Unidos", "United States", "Samsung Store", [SmartphoneCatalog, ApplianceCatalog, StorageCatalog, MonitorCatalog, HeadphonesCatalog, TvCatalog], ["Samsung"], 96, 1.01m, "2 days", "2 days", "1-year official", "1-year official", "Verified", "Verified", "https://www.samsung.com/us/search/searchMain/?searchTerm={0}", true),
        new("US", "Estados Unidos", "United States", "Best Buy", [], [], 94, 1.02m, "2 days", "2 days", "Manufacturer warranty", "Manufacturer warranty", "Verified", "Verified", "https://www.bestbuy.com/site/searchpage.jsp?st={0}", true),
        new("US", "Estados Unidos", "United States", "Amazon.com", [], [], 89, 0.98m, "2-5 days", "2-5 days", "Marketplace warranty", "Marketplace warranty", "Authorized", "Authorized", "https://www.amazon.com/s?k={0}", true),
        new("US", "Estados Unidos", "United States", "Walmart", [SmartphoneCatalog, ChargerCatalog, ApplianceCatalog, MouseCatalog, HeadphonesCatalog, TvCatalog, CoffeeMachineCatalog, PrinterCatalog, BabyCareCatalog, PetFoodCatalog, GeneralCatalog], [], 87, 0.99m, "2-5 days", "2-5 days", "Seller warranty", "Seller warranty", "Verified", "Verified", "https://www.walmart.com/search?q={0}", true),
        new("US", "Estados Unidos", "United States", "B&H Photo Video", [LaptopCatalog, SmartphoneCatalog, ChargerCatalog, MouseCatalog, RuggedTabletCatalog, StorageCatalog, MonitorCatalog, KeyboardCatalog, HeadphonesCatalog, TvCatalog, PrinterCatalog, GeneralCatalog], [], 93, 1.01m, "2-4 days", "2-4 days", "Manufacturer warranty", "Manufacturer warranty", "Verified", "Verified", "https://www.bhphotovideo.com/c/search?q={0}", true),
        new("US", "Estados Unidos", "United States", "Newegg", [LaptopCatalog, ChargerCatalog, MouseCatalog, RuggedTabletCatalog, StorageCatalog, MonitorCatalog, KeyboardCatalog, HeadphonesCatalog, PrinterCatalog, GeneralCatalog], [], 88, 0.99m, "2-5 days", "2-5 days", "Seller warranty", "Seller warranty", "Authorized", "Authorized", "https://www.newegg.com/p/pl?d={0}", true)
    };

    private static IReadOnlyDictionary<string, string> RetailerProductUrls { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [RetailerProductUrlKey("Radio Popular", "logitech-m650-signature")] = "https://www.radiopopular.pt/produto/rato-logitech-m650-graph"
    };

    private static IReadOnlyDictionary<string, RetailerProductOffer> RetailerProductOffers { get; } = new Dictionary<string, RetailerProductOffer>(StringComparer.OrdinalIgnoreCase)
    {
        [RetailerProductUrlKey("Radio Popular", "iphone-17")] = new(
            "https://www.radiopopular.pt/produto/apple-iphone-17-pro-max-256gb-lj",
            142499,
            "2-3 dias",
            "2-3 days"),
        [RetailerProductUrlKey("KuantoKusta", "logitech-g305-lightspeed")] = new(
            "https://www.kuantokusta.pt/p/199266/logitech-g305-lightspeed-wireless-gaming-910-005283",
            4298,
            "Confirmar loja",
            "Confirm store"),
        [RetailerProductUrlKey("PCDIGA", "logitech-g305-lightspeed")] = new(
            "https://www.pcdiga.com/rato-gaming-logitech-g305-lightspeed-wireless-preto-910-005282",
            5490,
            "Confirmar entrega",
            "Confirm delivery"),
        [RetailerProductUrlKey("Radio Popular", "logitech-g305-lightspeed")] = new(
            "https://www.radiopopular.pt/produto/rato-gaming-logitech-g305",
            5499,
            "2-3 dias",
            "2-3 days"),
        [RetailerProductUrlKey("Radio Popular", "logitech-m650-signature")] = new(
            "https://www.radiopopular.pt/produto/rato-logitech-m650-graph",
            3599,
            "3 dias",
            "3 days")
    };

    public IReadOnlyList<ProductResult> GetProducts(string? query)
    {
        var maxBudgetCents = TryExtractMaxBudgetCents(query);
        var products = ResolveProducts(query);
        if (maxBudgetCents.HasValue)
        {
            products = products
                .Where(product =>
                {
                    var priceCents = ParsePriceCents(product.Price);
                    return priceCents > 0 && priceCents <= maxBudgetCents.Value;
                })
                .ToList();
        }

        return LocalizeProducts(products
            .Select(product => ApplyBestKnownStorePrice(product, query))
            .Select(product => EnrichProduct(product, query))
            .ToList());
    }

    private ProductResult ApplyBestKnownStorePrice(ProductResult product, string? queryOrSlug)
    {
        var bestLiveOffer = BuildSellerOffersForProduct(product, queryOrSlug)
            .Where(offer => offer.IsLivePrice && offer.PriceCents > 0)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return bestLiveOffer is null
            ? product
            : product with { Price = bestLiveOffer.Price };
    }

    public ProductResult EnrichProduct(ProductResult product, string? query = null)
    {
        return product with
        {
            OfficialSource = BuildOfficialSource(product),
            OfficialSpecifications = BuildOfficialSpecifications(product),
            ReviewLinks = BuildReviewLinks(product, query)
        };
    }

    public IReadOnlyList<CriteriaWeight> GetCriteriaWeights(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return LaptopCriteria();
        }

        var normalizedQuery = NormalizeCatalogText(query);
        if (ContainsAnyTerm(normalizedQuery, ChargerCatalogTerms))
        {
            return ChargerCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "rato", "ratos", "mouse", "mice"))
        {
            return MouseCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, RuggedTabletCatalogTerms))
        {
            return RuggedTabletCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "cadeira", "cadeiras", "chair", "chairs"))
        {
            return ChairCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "teclado", "teclados", "keyboard", "keyboards"))
        {
            return KeyboardCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "monitor", "monitores", "display", "ecra", "ecran"))
        {
            return MonitorCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "auscultadores", "auriculares", "headphones", "headset", "earbuds"))
        {
            return HeadphonesCriteria();
        }

        return TryResolveCatalog(query.Trim(), out var catalog) switch
        {
            true when catalog == SmartphoneCatalog => SmartphoneCriteria(),
            true when catalog == ChargerCatalog => ChargerCriteria(),
            true when catalog == ApplianceCatalog => ApplianceCriteria(),
            true when catalog == MouseCatalog => MouseCriteria(),
            true when catalog == RuggedTabletCatalog => RuggedTabletCriteria(),
            true when catalog == LaptopCatalog => LaptopCriteria(),
            _ => GenericProductCriteria()
        };
    }

    public IReadOnlyList<SellerOffer> GetSellerOffers(string? queryOrSlug)
    {
        var product = ResolveOfferProduct(queryOrSlug);
        var offers = BuildSellerOffersForProduct(product, queryOrSlug);
        var maxBudgetCents = TryExtractMaxBudgetCents(queryOrSlug);
        return maxBudgetCents.HasValue
            ? offers.Where(offer => offer.PriceCents <= maxBudgetCents.Value).ToList()
            : offers;
    }

    public IReadOnlyList<SellerOffer> BuildSellerOffersForProduct(ProductResult product, string? queryOrSlug = null)
    {
        var catalog = ResolveOfferCatalog(product, queryOrSlug);
        var market = ResolveUserMarket();
        var marketRetailers = RetailerProfiles
            .Where(retailer => retailer.Market.Equals(market, StringComparison.OrdinalIgnoreCase))
            .Where(retailer => IsRetailerApplicable(retailer, product, catalog))
            .ToList();

        if (marketRetailers.Count == 0)
        {
            marketRetailers = RetailerProfiles
                .Where(retailer => retailer.Market.Equals("PT", StringComparison.OrdinalIgnoreCase))
                .Where(retailer => IsRetailerApplicable(retailer, product, catalog))
                .ToList();
        }

        var basePriceCents = Math.Max(1, ParsePriceCents(product.Price));
        if (basePriceCents <= 1)
        {
            basePriceCents = 9999;
        }

        var allOffers = marketRetailers
            .Select(retailer => BuildRetailerOffer(retailer, product, basePriceCents))
            .ToList();

        var liveOffers = allOffers
            .Where(offer => offer.IsLivePrice)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .ToList();

        var estimateLimit = Math.Max(0, 6 - liveOffers.Count);
        var offers = liveOffers
            .Concat(allOffers
                .Where(offer => !offer.IsLivePrice)
                .OrderBy(offer => offer.PriceCents)
                .ThenByDescending(offer => offer.ReliabilityScore)
                .Take(estimateLimit))
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .ToList();

        var preferred = offers
            .Where(offer => offer.IsLivePrice && offer.ReliabilityScore >= 80)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault()
            ?? offers
            .Where(offer => offer.ReliabilityScore >= 85)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault()
            ?? offers.OrderByDescending(offer => offer.ReliabilityScore).ThenBy(offer => offer.PriceCents).FirstOrDefault();

        var rankedOffers = offers
            .Select(offer => offer with { Preferred = preferred is not null && offer.Seller == preferred.Seller })
            .OrderByDescending(offer => offer.Preferred)
            .ThenBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .ToList();

        return LocalizeOffers(rankedOffers);
    }

    private IReadOnlyList<ProductResult> ResolveProducts(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return LaptopProducts;
        }

        var rawValue = query.Trim();
        var hasCatalog = TryResolveCatalog(rawValue, out var catalog);
        if (!hasCatalog && IsUnsupportedKnownCategory(NormalizeCatalogText(rawValue)))
        {
            return Array.Empty<ProductResult>();
        }

        var candidates = hasCatalog ? GetCatalogProducts(catalog) : AllProducts;
        if (TryFindKnownProductMatches(candidates, rawValue, out var knownProductMatches))
        {
            return knownProductMatches
                .Take(4)
                .Select((product, index) => product with { Rank = index + 1 })
                .ToList();
        }

        candidates = FilterProductsByRequestedSubtype(candidates, rawValue, catalog);
        var ranked = RankProducts(candidates, rawValue, hasCatalog && catalog != MarketplaceCatalog);

        if (ranked.Count > 0)
        {
            return ranked;
        }

        if (IsExplicitModelQuery(rawValue))
        {
            return Array.Empty<ProductResult>();
        }

        return hasCatalog ? candidates : Array.Empty<ProductResult>();
    }

    private static IReadOnlyList<ProductResult> GetCatalogProducts(string catalog)
    {
        return catalog switch
        {
            SmartphoneCatalog => SmartphoneProducts,
            ChargerCatalog => ChargerProducts,
            ApplianceCatalog => ApplianceProducts,
            MouseCatalog => MouseProducts,
            RuggedTabletCatalog => RuggedTabletProducts,
            TabletCatalog => TabletProducts,
            StorageCatalog => StorageProducts,
            MonitorCatalog => MonitorProducts,
            KeyboardCatalog => KeyboardProducts,
            HeadphonesCatalog => HeadphonesProducts,
            TvCatalog => TvProducts,
            CameraCatalog => CameraProducts,
            DroneCatalog => DroneProducts,
            CoffeeMachineCatalog => CoffeeMachineProducts,
            ToolCatalog => ToolProducts,
            ChairCatalog => ChairProducts,
            TyreCatalog => TyreProducts,
            PrinterCatalog => PrinterProducts,
            PetFoodCatalog => PetFoodProducts,
            BabyCareCatalog => BabyCareProducts,
            ToyCatalog => ToyProducts,
            HealthBeautyCatalog => HealthBeautyProducts,
            HomeCatalog => HomeProducts,
            SportCatalog => SportProducts,
            AutoCatalog => AutoProducts,
            OfficeCatalog => OfficeProducts,
            CultureFoodCatalog => CultureFoodProducts,
            MarketplaceCatalog => MarketplaceProducts,
            _ => LaptopProducts
        };
    }

    private static IReadOnlyList<ProductResult> FilterProductsByRequestedSubtype(
        IReadOnlyList<ProductResult> products,
        string query,
        string catalog)
    {
        if (catalog == MarketplaceCatalog)
        {
            var normalizedMarketplaceQuery = NormalizeCatalogText(query);
            if (ContainsAnyTerm(normalizedMarketplaceQuery, "ps5", "playstation"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "ps5", "playstation"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedMarketplaceQuery, "ps4", "comando", "dualshock", "dualsense"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "playstation", "ps5", "consola gaming"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedMarketplaceQuery, "nintendo", "switch"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "nintendo", "switch"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedMarketplaceQuery, "smartwatch", "relogio inteligente", "relógio inteligente")
                && ContainsAnyTerm(normalizedMarketplaceQuery, "android", "galaxy"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "android", "galaxy", "wear os"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedMarketplaceQuery, "apple watch", "iphone"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "apple watch", "watchos", "iphone"))
                    .ToList();
            }

            return products;
        }

        if (catalog == HealthBeautyCatalog)
        {
            var normalizedHealthQuery = NormalizeCatalogText(query);
            if (ContainsAnyTerm(normalizedHealthQuery, "perfume", "eau de parfum", "eau de toilette"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "perfume", "eau de toilette", "eau de parfum"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedHealthQuery, "creme", "serum", "shampoo", "protetor solar", "hidratante"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "creme", "serum", "shampoo", "protetor", "pele", "spf"))
                    .ToList();
            }

            return products;
        }

        if (catalog == CultureFoodCatalog)
        {
            var normalizedCultureQuery = NormalizeCatalogText(query);
            if (ContainsAnyTerm(normalizedCultureQuery, "vinho", "vinhos"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "vinho", "douro", "tinto"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedCultureQuery, "gin", "whisky", "rum", "champagne"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "gin", "whisky", "rum", "bebida espirituosa"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedCultureQuery, "manga"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "manga", "one piece"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedCultureQuery, "livro", "livros", "romance"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "livro", "isbn", "edicao"))
                    .ToList();
            }

            return products;
        }

        if (catalog == BabyCareCatalog)
        {
            var normalizedBabyQuery = NormalizeCatalogText(query);
            if (ContainsAnyTerm(normalizedBabyQuery, "fralda", "fraldas", "pampers"))
            {
                return products
                    .Where(product => ProductIdentityContainsAny(product, "fralda", "fraldas", "pampers"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedBabyQuery, "toalhita", "toalhitas", "dodot"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "toalhita", "toalhitas", "dodot"))
                    .ToList();
            }

            if (ContainsAnyTerm(normalizedBabyQuery, "berco", "berço", "chicco", "co-sleeping"))
            {
                return products
                    .Where(product => ProductContainsAny(product, "berco", "berço", "chicco", "co-sleeping"))
                    .ToList();
            }

            return products;
        }

        if (catalog != ApplianceCatalog)
        {
            return products;
        }

        var normalizedQuery = NormalizeCatalogText(query);
        if (ContainsAnyTerm(normalizedQuery, "frigorifico", "fridge", "geladeira", "combinado"))
        {
            return products
                .Where(product => ProductContainsAny(product, "frigorifico", "combinado", "frost", "frio"))
                .ToList();
        }

        if (ContainsAnyTerm(normalizedQuery, "lavadora", "lavagem", "maquina de lavar", "maquina lavar", "roupa"))
        {
            return products
                .Where(product => ProductContainsAny(product, "lavadora", "lavagem", "twindos", "roupa"))
                .ToList();
        }

        if (ContainsAnyTerm(normalizedQuery, "lava loica", "lava-loica", "loica", "loicas", "louca", "dishwasher"))
        {
            return products
                .Where(product => ProductContainsAny(product, "lava-loica", "loica", "loiça", "dishwasher"))
                .ToList();
        }

        if (ContainsAnyTerm(normalizedQuery, "forno", "fornos"))
        {
            return products
                .Where(product => ProductContainsAny(product, "forno", "encastre"))
                .ToList();
        }

        if (ContainsAnyTerm(normalizedQuery, "microondas"))
        {
            return products
                .Where(product => ProductContainsAny(product, "microondas"))
                .ToList();
        }

        if (ContainsAnyTerm(normalizedQuery, "balanca", "balancas"))
        {
            return products
                .Where(product => ProductContainsAny(product, "balanca", "scale"))
                .ToList();
        }

        return products;
    }

    private static bool ProductContainsAny(ProductResult product, params string[] terms)
    {
        var productText = NormalizeCatalogText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            product.AiSummary,
            string.Join(' ', product.Specs),
            string.Join(' ', product.Highlights)
        }));

        return terms.Any(term => ContainsCatalogTerm(productText, NormalizeCatalogText(term)));
    }

    private static bool ProductIdentityContainsAny(ProductResult product, params string[] terms)
    {
        var productText = NormalizeCatalogText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            string.Join(' ', product.Specs)
        }));

        return terms.Any(term => ContainsCatalogTerm(productText, NormalizeCatalogText(term)));
    }

    private static IReadOnlyList<ProductResult> RankProducts(
        IReadOnlyList<ProductResult> products,
        string query,
        bool includeUnmatchedProducts)
    {
        var normalizedQuery = NormalizeCatalogText(query);
        var searchTerms = BuildSearchTerms(normalizedQuery);
        var ranked = products
            .Select(product => new
            {
                Product = product,
                Relevance = ScoreProduct(product, normalizedQuery, searchTerms)
            })
            .OrderByDescending(item => item.Relevance)
            .ThenByDescending(item => item.Product.Score)
            .ToList();

        if (ranked.Count == 0 || ranked[0].Relevance <= 0)
        {
            return Array.Empty<ProductResult>();
        }

        if (TryFilterExactModelMatches(ranked.Select(item => item.Product).ToList(), normalizedQuery, out var exactModelMatches))
        {
            return exactModelMatches
                .Take(4)
                .Select((product, index) => product with { Rank = index + 1 })
                .ToList();
        }

        var matches = includeUnmatchedProducts
            ? ranked
            : ranked.Where(item => item.Relevance > 0);

        return matches
            .Take(4)
            .Select((item, index) => item.Product with { Rank = index + 1 })
            .ToList();
    }

    private static IReadOnlyList<string> BuildSearchTerms(string normalizedQuery)
    {
        var rawTerms = normalizedQuery
            .Replace('-', ' ')
            .Replace('/', ' ')
            .Replace(',', ' ')
            .Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "comprar",
            "compra",
            "procurar",
            "quero",
            "queria",
            "procuro",
            "para",
            "com",
            "sem",
            "uma",
            "um",
            "uns",
            "umas",
            "melhor",
            "best",
            "buy",
            "the",
            "and",
            "for",
            "with",
            "mais",
            "menos",
            "bom",
            "boa",
            "bons",
            "boas",
            "produto",
            "produtos",
            "vendedor",
            "revendedor",
            "autorizado",
            "autorizada",
            "seller",
            "authorized",
            "loja",
            "store",
            "ate",
            "até",
            "euro",
            "euros",
            "eur",
            "orçamento",
            "orcamento",
            "máximo",
            "maximo",
            "max",
            "ergonomico",
            "ergonomica",
            "confortavel",
            "confortavel"
        };

        return rawTerms
            .Where(term => term.Length > 2 && !stopWords.Contains(term))
            .SelectMany(ExpandSearchTerm)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ExpandSearchTerm(string term)
    {
        yield return term;

        foreach (var expanded in term switch
        {
            "barato" or "barata" or "economico" or "economica" or "baixo" or "baixa" or "cheap" => new[] { "preco", "valor", "competitivo", "agressivo" },
            "camera" or "camara" or "foto" or "fotografia" => new[] { "camera", "camara", "fotografia" },
            "bateria" or "autonomia" => new[] { "autonomia", "bateria", "mah", "video" },
            "leve" or "portatil" or "portabilidade" => new[] { "leve", "peso", "kg", "portabilidade" },
            "rapido" or "rapida" or "performance" or "jogos" or "gaming" => new[] { "performance", "snapdragon", "i7", "m3", "rapido" },
            "frio" or "frigorifico" or "fridge" or "geladeira" => new[] { "frigorifico", "combinado", "frost", "arrefecimento" },
            "roupa" or "lavar" or "lavagem" or "lavadora" => new[] { "lavadora", "lavagem", "maquina", "twindos" },
            "loica" or "loicas" or "louca" or "dishwasher" => new[] { "loica", "lava", "dishwasher" },
            "silencioso" or "silenciosa" or "ruido" => new[] { "silencioso", "ruido", "db" },
            "rato" or "ratos" or "mouse" or "mice" => new[] { "rato", "mouse", "sem fios", "bluetooth", "dpi" },
            "carregador" or "carregadores" or "charger" or "chargers" or "adaptador" => new[] { "carregador", "charger", "usb-c", "power delivery", "magsafe", "lightning" },
            "magsafe" or "lightning" or "powerbank" => new[] { "carregador", "magsafe", "lightning", "iphone", "usb-c" },
            "ergonomico" or "ergonomica" => new[] { "ergonomico", "confortavel", "formato" },
            "ipohone" => new[] { "iphone" },
            "tablet" or "tablets" or "tablete" or "tabela" or "touch" or "toutch" => new[] { "tablet", "touch", "10.1", "polegadas", "ecra" },
            "rugged" or "robusto" or "robusta" or "todoterreno" or "industrial" or "fabrica" => new[] { "rugged", "industrial", "ip66", "ip68", "mil-std", "fabrica" },
            "ipad" => new[] { "ipad", "tablet", "apple" },
            "tab" => new[] { "tablet", "galaxy tab", "android" },
            "disco" or "hdd" or "ssd" or "armazenamento" => new[] { "disco", "hdd", "ssd", "usb", "1tb", "2tb" },
            "monitor" or "display" => new[] { "monitor", "27", "qhd", "ips", "hz" },
            "teclado" or "keyboard" => new[] { "teclado", "keyboard", "switch", "bluetooth", "layout" },
            "auscultadores" or "auriculares" or "headphones" or "headset" or "earbuds" => new[] { "auscultadores", "bluetooth", "anc", "bateria" },
            "televisor" or "televisao" or "tv" => new[] { "tv", "55", "4k", "oled", "qled" },
            "cafe" or "espresso" => new[] { "cafe", "espresso", "maquina", "moinho", "capsulas" },
            "forno" => new[] { "forno", "encastre", "classe" },
            "microondas" => new[] { "microondas", "litros", "compacto" },
            "balanca" or "balancas" => new[] { "balanca", "scale", "app" },
            "berbequim" or "drill" or "aparafusadora" => new[] { "berbequim", "drill", "18v", "brushless", "percussao" },
            "serra" or "serras" => new[] { "serra", "ferramenta", "bosch", "makita", "dewalt" },
            "cadeira" or "chair" => new[] { "cadeira", "ergonomica", "lombar", "ajustavel" },
            "pneu" or "pneus" or "tyre" or "tyres" => new[] { "pneu", "205", "55", "r16", "verao" },
            "oleo" => new[] { "oleo", "motor", "5w30" },
            "impressora" or "printer" => new[] { "impressora", "multifuncoes", "wifi", "laser", "tinta" },
            "tinteiro" or "tinteiros" or "toner" => new[] { "tinteiro", "toner", "impressora", "cartucho" },
            "racao" or "cao" or "gato" or "dog" or "cat" => new[] { "racao", "cao", "gato", "kg", "adulto" },
            "ownat" or "acana" or "advance" or "orijen" or "hypoallergenic" => new[] { "racao", "cao", "gato", "adulto" },
            "fralda" or "fraldas" or "bebe" => new[] { "fralda", "bebe", "pampers", "dodot", "tamanho" },
            "lego" => new[] { "lego", "brinquedo", "construcao" },
            "mesa" or "desk" or "secretaria" => new[] { "mesa", "secretaria", "escritorio", "home office" },
            "colchao" or "sofa" or "candeeiro" or "tapete" => new[] { "casa", "decoracao", "colchao", "movel" },
            "aspirador" or "robot" => new[] { "aspirador", "robot", "limpeza", "mapeamento" },
            "consola" => new[] { "consola", "gaming" },
            "playstation" or "ps5" => new[] { "playstation", "ps5", "consola", "gaming" },
            "nintendo" => new[] { "nintendo", "switch", "consola", "gaming" },
            "smartwatch" or "relogio" => new[] { "smartwatch", "relogio", "gps", "saude" },
            "vigilancia" => new[] { "vigilancia", "camera", "wi-fi", "noturna" },
            "bicicleta" or "trotinete" or "mobilidade" => new[] { "bicicleta", "trotinete", "eletrica", "mobilidade" },
            "comprimidos" or "capsulas" or "saquetas" or "suplemento" or "magnesio" => new[] { "comprimidos", "suplemento", "saude", "farmacia" },
            "creme" or "serum" or "shampoo" or "protetor" or "perfume" => new[] { "creme", "perfume", "pele", "dermocosmetica" },
            "livro" or "livros" or "manga" => new[] { "livro", "manga", "isbn", "edicao" },
            "vinho" or "vinhos" or "gin" or "whisky" or "rum" => new[] { "vinho", "gin", "bebida", "garrafa" },
            "caderno" or "caneta" or "papel" or "calculadora" => new[] { "papelaria", "escritorio", "material" },
            _ => Array.Empty<string>()
        })
        {
            yield return expanded;
        }
    }

    private static int ScoreProduct(ProductResult product, string normalizedQuery, IReadOnlyList<string> searchTerms)
    {
        var slug = NormalizeCatalogText(product.Slug);
        var name = NormalizeCatalogText(product.Name);
        var brand = NormalizeCatalogText(product.Brand);
        var specs = NormalizeCatalogText(string.Join(' ', product.Specs));
        var highlights = NormalizeCatalogText(string.Join(' ', product.Highlights));
        var badge = NormalizeCatalogText(product.Badge);
        var summary = NormalizeCatalogText(product.AiSummary);

        var score = 0;
        if (normalizedQuery.Contains(name, StringComparison.OrdinalIgnoreCase)
            || name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }

        foreach (var term in searchTerms)
        {
            if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 18;
            }

            if (slug.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 14;
            }

            if (brand.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 12;
            }

            if (highlights.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
            }

            if (summary.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
            }

            if (specs.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 6;
            }

            if (badge.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 5;
            }
        }

        score += ScoreScreenSizePreference(normalizedQuery, name, specs);

        return score;
    }

    private static int ScoreScreenSizePreference(string normalizedQuery, string name, string specs)
    {
        var productText = $"{name} {specs}";

        if (ContainsScreenSizeRequest(normalizedQuery, "14"))
        {
            return ProductMatchesScreenSize(productText, "14") ? 24 : -14;
        }

        if (ContainsScreenSizeRequest(normalizedQuery, "10"))
        {
            return ProductMatchesScreenSize(productText, "10") || productText.Contains("10.1", StringComparison.OrdinalIgnoreCase)
                ? 18
                : -10;
        }

        if (ContainsScreenSizeRequest(normalizedQuery, "27"))
        {
            return ProductMatchesScreenSize(productText, "27") ? 20 : -10;
        }

        if (ContainsScreenSizeRequest(normalizedQuery, "55"))
        {
            return ProductMatchesScreenSize(productText, "55") ? 20 : -10;
        }

        return 0;
    }

    private static bool ContainsScreenSizeRequest(string normalizedQuery, string size)
    {
        return ContainsCatalogTerm(normalizedQuery, size)
            || normalizedQuery.Contains($"{size}\"", StringComparison.OrdinalIgnoreCase)
            || normalizedQuery.Contains($"{size} polegadas", StringComparison.OrdinalIgnoreCase)
            || normalizedQuery.Contains($"{size} inch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProductMatchesScreenSize(string productText, string size)
    {
        return ContainsCatalogTerm(productText, size)
            || productText.Contains($"{size}\"", StringComparison.OrdinalIgnoreCase)
            || productText.Contains($"{size}.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryFilterExactModelMatches(
        IReadOnlyList<ProductResult> products,
        string normalizedQuery,
        out IReadOnlyList<ProductResult> matches)
    {
        matches = Array.Empty<ProductResult>();
        var modelNumbers = ExtractExplicitModelNumbers(normalizedQuery);
        if (modelNumbers.Count == 0)
        {
            return false;
        }

        var variantTerms = ExtractExplicitVariantTerms(normalizedQuery);
        foreach (var family in ExactModelFamilies)
        {
            if (!family.Aliases.Any(alias => ContainsCatalogTerm(normalizedQuery, alias)))
            {
                continue;
            }

            matches = products
                .Where(product => ProductMatchesExactModel(product, family.Canonical, modelNumbers, variantTerms))
                .ToList();

            return true;
        }

        return false;
    }

    private static bool IsExplicitModelQuery(string query)
    {
        var normalizedQuery = NormalizeCatalogText(query);
        return ExtractExplicitModelNumbers(normalizedQuery).Count > 0
            && ExactModelFamilies.Any(family =>
                family.Aliases.Any(alias => ContainsCatalogTerm(normalizedQuery, alias)));
    }

    private static IReadOnlyList<string> ExtractExplicitModelNumbers(string normalizedQuery)
    {
        var modelNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in GetCatalogTokens(normalizedQuery))
        {
            if (token.All(char.IsDigit) && token.Length <= 2)
            {
                modelNumbers.Add(token);
                continue;
            }

            if (token.Length is >= 2 and <= 4
                && token[0] is 's'
                && token.Skip(1).All(char.IsDigit))
            {
                modelNumbers.Add(token[1..]);
            }

            if (token.Length is >= 2 and <= 4
                && token[0] is 'm'
                && token.Skip(1).All(char.IsDigit))
            {
                modelNumbers.Add(token[1..]);
            }
        }

        return modelNumbers.ToList();
    }

    private static IReadOnlyList<string> ExtractExplicitVariantTerms(string normalizedQuery)
    {
        var tokens = GetCatalogTokens(normalizedQuery).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new[] { "pro", "max", "plus", "air" }
            .Where(tokens.Contains)
            .ToList();
    }

    private static bool ProductMatchesExactModel(
        ProductResult product,
        string family,
        IReadOnlyList<string> modelNumbers,
        IReadOnlyList<string> variantTerms)
    {
        var productText = NormalizeCatalogText(family is "iphone" or "pixel"
            ? $"{product.Name} {product.Slug}"
            : $"{product.Name} {product.Slug} {string.Join(' ', product.Specs)}");
        if (!ContainsCatalogTerm(productText, family))
        {
            return false;
        }

        if (variantTerms.Count > 0 && variantTerms.Any(term => !ContainsCatalogTerm(productText, term)))
        {
            return false;
        }

        var productTokens = GetCatalogTokens(productText).ToList();
        return modelNumbers.Any(modelNumber =>
            productTokens.Any(token => TokenContainsModelNumber(token, modelNumber)));
    }

    private static bool TokenContainsModelNumber(string token, string modelNumber)
    {
        return token.Equals(modelNumber, StringComparison.OrdinalIgnoreCase)
            || (token.Length <= modelNumber.Length + 8
                && token.EndsWith(modelNumber, StringComparison.OrdinalIgnoreCase)
                && token.Any(char.IsLetter));
    }

    public ProductResult? FindProduct(string slug)
    {
        var product = AllProducts
            .FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return product is null ? null : LocalizeProduct(EnrichProduct(product, slug));
    }

    public SellerOffer GetBestOffer(string? queryOrSlug = null)
    {
        return GetSellerOffers(queryOrSlug)
            .OrderByDescending(offer => offer.Preferred)
            .ThenBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .First();
    }

    private string ResolveCatalog(string? queryOrSlug)
    {
        return !string.IsNullOrWhiteSpace(queryOrSlug) && TryResolveCatalog(queryOrSlug.Trim(), out var catalog)
            ? catalog
            : LaptopCatalog;
    }

    private ProductResult ResolveOfferProduct(string? queryOrSlug)
    {
        if (!string.IsNullOrWhiteSpace(queryOrSlug))
        {
            var normalized = NormalizeCatalogText(queryOrSlug);
            var exactProduct = AllProducts.FirstOrDefault(product =>
                NormalizeCatalogText(product.Slug).Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || NormalizeCatalogText(product.Name).Equals(normalized, StringComparison.OrdinalIgnoreCase));

            if (exactProduct is not null)
            {
                return exactProduct;
            }

            var rankedProduct = ResolveProducts(queryOrSlug).FirstOrDefault();
            if (rankedProduct is not null)
            {
                return rankedProduct;
            }
        }

        return LaptopProducts[0];
    }

    private string ResolveOfferCatalog(ProductResult product, string? queryOrSlug)
    {
        if (SmartphoneProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return SmartphoneCatalog;
        }

        if (ChargerProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return ChargerCatalog;
        }

        if (ApplianceProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return ApplianceCatalog;
        }

        if (MouseProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return MouseCatalog;
        }

        if (RuggedTabletProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return RuggedTabletCatalog;
        }

        if (TabletProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return TabletCatalog;
        }

        if (StorageProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return StorageCatalog;
        }

        if (MonitorProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return MonitorCatalog;
        }

        if (KeyboardProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return KeyboardCatalog;
        }

        if (HeadphonesProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return HeadphonesCatalog;
        }

        if (TvProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return TvCatalog;
        }

        if (CameraProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return CameraCatalog;
        }

        if (DroneProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return DroneCatalog;
        }

        if (CoffeeMachineProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return CoffeeMachineCatalog;
        }

        if (ToolProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return ToolCatalog;
        }

        if (ChairProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return ChairCatalog;
        }

        if (TyreProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return TyreCatalog;
        }

        if (PrinterProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return PrinterCatalog;
        }

        if (PetFoodProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return PetFoodCatalog;
        }

        if (BabyCareProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return BabyCareCatalog;
        }

        if (ToyProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return ToyCatalog;
        }

        if (HealthBeautyProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return HealthBeautyCatalog;
        }

        if (HomeProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return HomeCatalog;
        }

        if (SportProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return SportCatalog;
        }

        if (AutoProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return AutoCatalog;
        }

        if (OfficeProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return OfficeCatalog;
        }

        if (CultureFoodProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return CultureFoodCatalog;
        }

        if (MarketplaceProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return MarketplaceCatalog;
        }

        if (LaptopProducts.Any(item => item.Slug.Equals(product.Slug, StringComparison.OrdinalIgnoreCase)))
        {
            return LaptopCatalog;
        }

        if (!string.IsNullOrWhiteSpace(queryOrSlug) && TryResolveCatalog(queryOrSlug.Trim(), out var catalog))
        {
            return catalog;
        }

        return GeneralCatalog;
    }

    private static string ResolveUserMarket()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase)
            || culture.Name.EndsWith("-US", StringComparison.OrdinalIgnoreCase)
            || culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return "US";
        }

        return "PT";
    }

    private static bool IsRetailerApplicable(RetailerProfile retailer, ProductResult product, string catalog)
    {
        var catalogMatches = retailer.Catalogs.Count == 0
            || retailer.Catalogs.Contains(catalog, StringComparer.OrdinalIgnoreCase);
        if (!catalogMatches)
        {
            return false;
        }

        return retailer.Brands.Count == 0
            || retailer.Brands.Contains(product.Brand, StringComparer.OrdinalIgnoreCase);
    }

    private SellerOffer BuildRetailerOffer(RetailerProfile retailer, ProductResult product, long basePriceCents)
    {
        var query = Uri.EscapeDataString(product.Name);
        var hasKnownOffer = RetailerProductOffers.TryGetValue(RetailerProductUrlKey(retailer.Name, product.Slug), out var knownOffer);
        var productUrl = hasKnownOffer
            ? knownOffer!.Url
            : RetailerProductUrls.TryGetValue(RetailerProductUrlKey(retailer.Name, product.Slug), out var directUrl)
            ? directUrl
            : string.Format(CultureInfo.InvariantCulture, retailer.UrlFormat, query);
        var priceCents = hasKnownOffer
            ? knownOffer!.PriceCents
            : ApplyRetailerPrice(basePriceCents, retailer.PriceFactor);
        var evidence = hasKnownOffer
            ? text.Pick(
                "Preço recolhido numa página de produto conhecida. Confirma stock e preço final antes de pagar.",
                "Price collected from a known product page. Confirm stock and final price before paying.")
            : retailer.DirectSeller
            ? text.Pick(
                "Loja conhecida na tua regiao. Abre a pesquisa para confirmar preco final, stock e vendedor.",
                "Known store in your region. Open the search to confirm final price, stock and seller.")
            : text.Pick(
                "Comparador local. Confirma a loja final antes de comprar.",
                "Local comparison source. Confirm the final seller before buying.");

        return new SellerOffer(
            retailer.Name,
            FormatCurrency(priceCents),
            priceCents,
            text.Pick(knownOffer?.DeliveryPt ?? retailer.DeliveryPt, knownOffer?.DeliveryEn ?? retailer.DeliveryEn),
            text.Pick(retailer.WarrantyPt, retailer.WarrantyEn),
            text.Pick(retailer.StatusPt, retailer.StatusEn),
            productUrl,
            false,
            retailer.ReliabilityScore,
            text.Pick(retailer.LocationPt, retailer.LocationEn),
            evidence,
            hasKnownOffer);
    }

    private static string RetailerProductUrlKey(string retailerName, string productSlug)
    {
        return $"{retailerName}|{productSlug}";
    }

    public static bool IsConfirmedStoreOffer(SellerOffer offer)
    {
        return offer.IsLivePrice && HasDirectProductUrl(offer.Url);
    }

    private static bool HasDirectProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        var path = parsed.AbsolutePath;
        if (string.IsNullOrWhiteSpace(path) || path.Equals("/", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.Contains("/pesquisa", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/search", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/SearchResult", StringComparison.OrdinalIgnoreCase)
            && !parsed.Query.Contains("q=", StringComparison.OrdinalIgnoreCase)
            && !parsed.Query.Contains("k=", StringComparison.OrdinalIgnoreCase)
            && !parsed.Query.Contains("query=", StringComparison.OrdinalIgnoreCase)
            && !parsed.Query.Contains("search=", StringComparison.OrdinalIgnoreCase)
            && !parsed.Query.Contains("searchvalue=", StringComparison.OrdinalIgnoreCase);
    }

    private static long ApplyRetailerPrice(long basePriceCents, decimal priceFactor)
    {
        var adjusted = Math.Max(1, (long)Math.Round(basePriceCents * priceFactor, MidpointRounding.AwayFromZero));
        return adjusted >= 10000
            ? (long)Math.Round(adjusted / 100m, MidpointRounding.AwayFromZero) * 100
            : adjusted;
    }

    private static long ParsePriceCents(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var digits = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsDigit(character) || character is ',' or '.')
            {
                digits.Append(character);
            }
        }

        var amount = digits.ToString();
        if (string.IsNullOrWhiteSpace(amount))
        {
            return 0;
        }

        if (amount.Contains(',', StringComparison.Ordinal))
        {
            amount = amount.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (amount.Contains('.', StringComparison.Ordinal)
            && amount[(amount.LastIndexOf('.') + 1)..].Length == 3)
        {
            amount = amount.Replace(".", string.Empty);
        }
        else if (amount.Count(character => character == '.') > 1)
        {
            var lastDot = amount.LastIndexOf('.');
            amount = amount[..lastDot].Replace(".", string.Empty) + amount[lastDot..];
        }

        return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? (long)Math.Round(parsed * 100m, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static string FormatCurrency(long cents)
    {
        return $"{(cents / 100m).ToString("N2", CultureInfo.CurrentCulture)} €";
    }

    private static long? TryExtractMaxBudgetCents(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalized = NormalizeCatalogText(query);
        var match = BudgetRegex.Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        var rawAmount = match.Groups["amount"].Value.Replace(',', '.');
        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)
            : null;
    }

    private static readonly Regex BudgetRegex = new(
        @"(?:(?:ate|under|below)\s*(?:de\s*)?(?<amount>\d+(?:[\.,]\d{1,2})?)\s*(?:eur|euros?|\u20ac))|(?:(?:preco|valor|budget|orcamento|maximo|max)\s*(?:de\s*)?(?:eur|euros?|\u20ac)?\s*(?<amount>\d+(?:[\.,]\d{1,2})?))|(?<amount>\d+(?:[\.,]\d{1,2})?)\s*(?:eur|euros?|\u20ac)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static bool TryResolveCatalog(string rawValue, out string catalog)
    {
        var value = NormalizeCatalogText(rawValue);

        if (ContainsAnyTerm(value, "berbequim", "aparafusadora", "serra", "dewalt", "makita", "bosch professional", "rebarbadora", "martelo")
            && !ContainsAnyTerm(value, "usb-c", "iphone", "magsafe", "lightning", "powerbank", "power bank"))
        {
            catalog = ToolCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "carrinho bebe", "carrinho baby", "cadeira bebe", "cadeira baby", "yoyo", "bebe", "baby")
            && ContainsAnyTerm(value, "adaptador", "suporte", "copo", "acoplador", "base"))
        {
            catalog = BabyCareCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, ChargerCatalogTerms))
        {
            catalog = ChargerCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "camera vigilancia", "camara vigilancia", "vigilancia interior", "seguranca interior"))
        {
            catalog = MarketplaceCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "smartphone", "telemovel", "celular", "iphone", "redmi", "xiaomi smartphone")
            && ContainsAnyTerm(value, "camera", "camara", "lente"))
        {
            catalog = SmartphoneCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "maquina de cafe", "maquina cafe", "capsulas cafe", "nespresso", "delonghi", "dolce gusto"))
        {
            catalog = CoffeeMachineCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "camera", "camara")
            && ContainsAnyTerm(value, "suporte longo", "atualizacoes", "actualizacoes", "vendedor autorizado", "bateria"))
        {
            catalog = SmartphoneCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, DroneCatalogTerms))
        {
            catalog = DroneCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "camera digital", "camara digital", "maquina fotografica", "fotografica", "canon eos", "fujifilm", "instax", "gopro", "sony alpha", "xplorer dv"))
        {
            catalog = CameraCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "galaxy buds", "buds", "soundbar", "barra de som"))
        {
            catalog = HeadphonesCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "smartwatch", "smart watch", "smartband", "smart band", "galaxy watch", "galaxy fit", "pulseira desportiva"))
        {
            catalog = MarketplaceCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, RuggedTabletCatalogTerms))
        {
            catalog = RuggedTabletCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "galaxy tab", "redmi pad", "redmi se", "xiaomi pad", "ipad", "lenovo tab", "idea tab")
            || ContainsCatalogTerm(value, "tablet"))
        {
            catalog = TabletCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "disco externo", "ssd externo", "hdd externo", "external hard", "external ssd", "portable high speed", "type-c 3"))
        {
            catalog = StorageCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "portatil asus", "portatil gaming", "chromebook", "lenovo legion", "hp omen", "macbook", "zenbook", "thinkpad"))
        {
            catalog = LaptopCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "teclado", "keyboard"))
        {
            catalog = KeyboardCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "rato", "mouse", "tapete de rato"))
        {
            catalog = MouseCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "auscultadores", "auriculares", "headset", "headphones", "earphones"))
        {
            catalog = HeadphonesCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "portatil", "laptop", "notebook", "macbook", "zenbook", "thinkpad"))
        {
            catalog = LaptopCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "body mist", "perfume", "eau de parfum", "eau de toilette", "creme facial", "oleo bronzeador", "bronzeador", "autobronzeadora", "australian gold", "cocosolis", "protetor solar", "shampoo", "retinol"))
        {
            catalog = HealthBeautyCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "cadeira auto", "toalhitas bebe", "toalhitas baby", "fraldas", "dodot", "pampers", "carrinho bebe", "bebeconfort"))
        {
            catalog = BabyCareCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "tinta aquosa", "tinta acrilica", "tinta acrilex", "robbi", "dyrup", "bostik", "silicone", "sanitarios", "pulverizador de tinta", "maquina de pintura", "cortar sulcos", "cavalete", "bicarbonato", "repelente ultrassonico"))
        {
            catalog = ToolCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "lego", "furby", "boneca", "reborn", "brinquedo", "jogo de tabuleiro", "jogo tabuleiro", "jogo da velha", "hot wheels"))
        {
            catalog = ToyCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "ps5", "playstation", "nintendo switch", "xbox", "comando ps4", "comando playstation", "dualshock", "dualsense", "gaming"))
        {
            catalog = MarketplaceCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "robot aspirador", "aspirador robot", "roomba", "roborock", "irobot"))
        {
            catalog = MarketplaceCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "aspirador", "fritadeira", "air fryer", "maquina limpeza", "limpeza de texteis"))
        {
            catalog = ApplianceCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "calculadora"))
        {
            catalog = OfficeCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "serra", "serras", "berbequim", "aparafusadora"))
        {
            catalog = ToolCatalog;
            return true;
        }

        if (ContainsAnyTerm(value, "cafe em grao", "capsulas cafe", "delta q", "kaffa", "dolce gusto", "nescafe", "kimbo"))
        {
            catalog = CultureFoodCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, SmartphoneProducts))
        {
            catalog = SmartphoneCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ChargerProducts))
        {
            catalog = ChargerCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ApplianceProducts))
        {
            catalog = ApplianceCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, MouseProducts))
        {
            catalog = MouseCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, RuggedTabletProducts))
        {
            catalog = RuggedTabletCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, TabletProducts))
        {
            catalog = TabletCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, StorageProducts))
        {
            catalog = StorageCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, MonitorProducts))
        {
            catalog = MonitorCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, KeyboardProducts))
        {
            catalog = KeyboardCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, HeadphonesProducts))
        {
            catalog = HeadphonesCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, TvProducts))
        {
            catalog = TvCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, DroneProducts))
        {
            catalog = DroneCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, CameraProducts))
        {
            catalog = CameraCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, CoffeeMachineProducts))
        {
            catalog = CoffeeMachineCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ToolProducts))
        {
            catalog = ToolCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ChairProducts))
        {
            catalog = ChairCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, TyreProducts))
        {
            catalog = TyreCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, PrinterProducts))
        {
            catalog = PrinterCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, PetFoodProducts))
        {
            catalog = PetFoodCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, BabyCareProducts))
        {
            catalog = BabyCareCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ToyProducts))
        {
            catalog = ToyCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, HealthBeautyProducts))
        {
            catalog = HealthBeautyCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, HomeProducts))
        {
            catalog = HomeCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, SportProducts))
        {
            catalog = SportCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, AutoProducts))
        {
            catalog = AutoCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, OfficeProducts))
        {
            catalog = OfficeCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, CultureFoodProducts))
        {
            catalog = CultureFoodCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, MarketplaceProducts))
        {
            catalog = MarketplaceCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, LaptopProducts))
        {
            catalog = LaptopCatalog;
            return true;
        }

        var scores = new[]
        {
            new { Catalog = SmartphoneCatalog, Score = ScoreCatalog(value, SmartphoneCatalogTerms) },
            new { Catalog = ChargerCatalog, Score = ScoreCatalog(value, ChargerCatalogTerms) },
            new { Catalog = ApplianceCatalog, Score = ScoreCatalog(value, ApplianceCatalogTerms) },
            new { Catalog = MouseCatalog, Score = ScoreCatalog(value, MouseCatalogTerms) },
            new { Catalog = RuggedTabletCatalog, Score = ScoreCatalog(value, RuggedTabletCatalogTerms) },
            new { Catalog = TabletCatalog, Score = ScoreCatalog(value, TabletCatalogTerms) },
            new { Catalog = StorageCatalog, Score = ScoreCatalog(value, StorageCatalogTerms) },
            new { Catalog = MonitorCatalog, Score = ScoreCatalog(value, MonitorCatalogTerms) },
            new { Catalog = KeyboardCatalog, Score = ScoreCatalog(value, KeyboardCatalogTerms) },
            new { Catalog = HeadphonesCatalog, Score = ScoreCatalog(value, HeadphonesCatalogTerms) },
            new { Catalog = TvCatalog, Score = ScoreCatalog(value, TvCatalogTerms) },
            new { Catalog = DroneCatalog, Score = ScoreCatalog(value, DroneCatalogTerms) },
            new { Catalog = CameraCatalog, Score = ScoreCatalog(value, CameraCatalogTerms) },
            new { Catalog = CoffeeMachineCatalog, Score = ScoreCatalog(value, CoffeeMachineCatalogTerms) },
            new { Catalog = ToolCatalog, Score = ScoreCatalog(value, ToolCatalogTerms) },
            new { Catalog = ChairCatalog, Score = ScoreCatalog(value, ChairCatalogTerms) },
            new { Catalog = TyreCatalog, Score = ScoreCatalog(value, TyreCatalogTerms) },
            new { Catalog = PrinterCatalog, Score = ScoreCatalog(value, PrinterCatalogTerms) },
            new { Catalog = PetFoodCatalog, Score = ScoreCatalog(value, PetFoodCatalogTerms) },
            new { Catalog = BabyCareCatalog, Score = ScoreCatalog(value, BabyCareCatalogTerms) },
            new { Catalog = ToyCatalog, Score = ScoreCatalog(value, ToyCatalogTerms) },
            new { Catalog = HealthBeautyCatalog, Score = ScoreCatalog(value, HealthBeautyCatalogTerms) },
            new { Catalog = HomeCatalog, Score = ScoreCatalog(value, HomeCatalogTerms) },
            new { Catalog = SportCatalog, Score = ScoreCatalog(value, SportCatalogTerms) },
            new { Catalog = AutoCatalog, Score = ScoreCatalog(value, AutoCatalogTerms) },
            new { Catalog = OfficeCatalog, Score = ScoreCatalog(value, OfficeCatalogTerms) },
            new { Catalog = CultureFoodCatalog, Score = ScoreCatalog(value, CultureFoodCatalogTerms) },
            new { Catalog = MarketplaceCatalog, Score = ScoreCatalog(value, MarketplaceCatalogTerms) },
            new { Catalog = LaptopCatalog, Score = ScoreCatalog(value, LaptopCatalogTerms) }
        }
        .OrderByDescending(item => item.Score)
        .ToList();

        if (scores[0].Score <= 0)
        {
            catalog = LaptopCatalog;
            return false;
        }

        catalog = scores[0].Catalog;
        return true;
    }

    private IReadOnlyList<CriteriaWeight> LaptopCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Battery life", 95, "#5EE9A8"), ("Weight and portability", 70, "#7BE8E0"), ("Performance (CPU)", 60, "#7BE8E0"), ("Display quality", 50, "#B49CFF"), ("Warranty and support", 75, "#5EE9A8"), ("Sustainability", 40, "#E9D67B"), ("Origin / manufacturing", 30, "#F0A36A"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Autonomia", 95, "#5EE9A8"), ("Peso e portabilidade", 70, "#7BE8E0"), ("Performance (CPU)", 60, "#7BE8E0"), ("Qualidade do ecrã", 50, "#B49CFF"), ("Garantia e suporte", 75, "#5EE9A8"), ("Sustentabilidade", 40, "#E9D67B"), ("Origem / fabrico", 30, "#F0A36A"));
    }

    private IReadOnlyList<CriteriaWeight> SmartphoneCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Camera quality", 92, "#B49CFF"), ("Battery life", 85, "#5EE9A8"), ("Updates and support", 88, "#5EE9A8"), ("Display", 72, "#7BE8E0"), ("Performance", 66, "#7BE8E0"), ("Storage", 55, "#E9D67B"), ("Warranty / authorized seller", 82, "#F0A36A"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Qualidade da câmara", 92, "#B49CFF"), ("Bateria", 85, "#5EE9A8"), ("Atualizações e suporte", 88, "#5EE9A8"), ("Ecrã", 72, "#7BE8E0"), ("Performance", 66, "#7BE8E0"), ("Armazenamento", 55, "#E9D67B"), ("Garantia / vendedor autorizado", 82, "#F0A36A"));
    }

    private IReadOnlyList<CriteriaWeight> ChargerCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Compatibility", 95, "#5EE9A8"), ("Power", 88, "#7BE8E0"), ("Safety certification", 92, "#B49CFF"), ("Price", 82, "#7BE8E0"), ("Cable included", 62, "#E9D67B"), ("Warranty / seller", 78, "#F0A36A"))
            : BuildCriteria(("Compatibilidade", 95, "#5EE9A8"), ("Potencia", 88, "#7BE8E0"), ("Certificacao de seguranca", 92, "#B49CFF"), ("Preco", 82, "#7BE8E0"), ("Cabo incluido", 62, "#E9D67B"), ("Garantia / vendedor", 78, "#F0A36A"));
    }

    private IReadOnlyList<CriteriaWeight> ApplianceCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 75, "#7BE8E0"), ("Energy efficiency", 95, "#5EE9A8"), ("Capacity", 85, "#7BE8E0"), ("Noise level", 70, "#B49CFF"), ("Annual consumption", 85, "#5EE9A8"), ("Dimensions / installation", 65, "#E9D67B"), ("Warranty / assistance", 82, "#F0A36A"), ("Delivery / removal", 60, "#7BE8E0"))
            : BuildCriteria(("Preço", 75, "#7BE8E0"), ("Eficiência energética", 95, "#5EE9A8"), ("Capacidade", 85, "#7BE8E0"), ("Nível de ruído", 70, "#B49CFF"), ("Consumo anual", 85, "#5EE9A8"), ("Dimensões / instalação", 65, "#E9D67B"), ("Garantia / assistência", 82, "#F0A36A"), ("Entrega / recolha", 60, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> MouseCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Ergonomics", 90, "#5EE9A8"), ("Sensor / DPI", 78, "#B49CFF"), ("Connectivity", 72, "#7BE8E0"), ("Battery / cable", 65, "#5EE9A8"), ("Weight and size", 60, "#E9D67B"), ("Click noise", 45, "#F0A36A"), ("Warranty / seller", 75, "#7BE8E0"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Ergonomia", 90, "#5EE9A8"), ("Sensor / DPI", 78, "#B49CFF"), ("Conectividade", 72, "#7BE8E0"), ("Bateria / cabo", 65, "#5EE9A8"), ("Peso e tamanho", 60, "#E9D67B"), ("Ruído dos cliques", 45, "#F0A36A"), ("Garantia / vendedor", 75, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> RuggedTabletCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Rugged certification", 95, "#5EE9A8"), ("Screen / resolution", 92, "#7BE8E0"), ("Touch with gloves", 88, "#B49CFF"), ("OS compatibility", 90, "#5EE9A8"), ("Battery / dock", 78, "#E9D67B"), ("Connectivity", 74, "#7BE8E0"), ("Support / accessories", 82, "#F0A36A"), ("Price", 62, "#7BE8E0"))
            : BuildCriteria(("Certificação rugged", 95, "#5EE9A8"), ("Ecrã / resolução", 92, "#7BE8E0"), ("Toque com luvas", 88, "#B49CFF"), ("Compatibilidade do software", 90, "#5EE9A8"), ("Bateria / dock", 78, "#E9D67B"), ("Conectividade", 74, "#7BE8E0"), ("Suporte / acessórios", 82, "#F0A36A"), ("Preço", 62, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> ChairCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Ergonomics / lumbar support", 95, "#5EE9A8"), ("Adjustability", 85, "#7BE8E0"), ("Materials", 70, "#B49CFF"), ("Size / supported weight", 72, "#E9D67B"), ("Daily comfort", 90, "#5EE9A8"), ("Assembly / delivery", 55, "#F0A36A"), ("Warranty / returns", 75, "#7BE8E0"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Ergonomia / apoio lombar", 95, "#5EE9A8"), ("Ajustes", 85, "#7BE8E0"), ("Materiais", 70, "#B49CFF"), ("Dimensões / peso suportado", 72, "#E9D67B"), ("Conforto diário", 90, "#5EE9A8"), ("Montagem / entrega", 55, "#F0A36A"), ("Garantia / devolução", 75, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> KeyboardCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Layout / compatibility", 90, "#5EE9A8"), ("Switch type", 82, "#B49CFF"), ("Ergonomics", 75, "#7BE8E0"), ("Connectivity", 70, "#5EE9A8"), ("Noise", 45, "#F0A36A"), ("Build quality", 80, "#E9D67B"), ("Warranty / seller", 72, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Layout / compatibilidade", 90, "#5EE9A8"), ("Tipo de switch", 82, "#B49CFF"), ("Ergonomia", 75, "#7BE8E0"), ("Conectividade", 70, "#5EE9A8"), ("Ruído", 45, "#F0A36A"), ("Qualidade de construção", 80, "#E9D67B"), ("Garantia / vendedor", 72, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> MonitorCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Size / resolution", 92, "#5EE9A8"), ("Panel type", 84, "#B49CFF"), ("Refresh rate", 72, "#7BE8E0"), ("Ergonomics", 60, "#E9D67B"), ("Connectivity", 75, "#5EE9A8"), ("Consumption", 45, "#F0A36A"), ("Warranty / pixels", 80, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Tamanho / resolução", 92, "#5EE9A8"), ("Tipo de painel", 84, "#B49CFF"), ("Taxa de atualização", 72, "#7BE8E0"), ("Ergonomia", 60, "#E9D67B"), ("Conectividade", 75, "#5EE9A8"), ("Consumo", 45, "#F0A36A"), ("Garantia / pixels", 80, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> HeadphonesCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Sound quality", 92, "#5EE9A8"), ("ANC / isolation", 82, "#B49CFF"), ("Comfort", 88, "#7BE8E0"), ("Battery", 78, "#5EE9A8"), ("Microphone", 65, "#E9D67B"), ("Codec / connectivity", 72, "#F0A36A"), ("Warranty / seller", 70, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Qualidade de som", 92, "#5EE9A8"), ("ANC / isolamento", 82, "#B49CFF"), ("Conforto", 88, "#7BE8E0"), ("Bateria", 78, "#5EE9A8"), ("Microfone", 65, "#E9D67B"), ("Codec / conectividade", 72, "#F0A36A"), ("Garantia / vendedor", 70, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> GenericProductCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 82, "#7BE8E0"), ("Warranty", 78, "#5EE9A8"), ("Authenticity", 88, "#B49CFF"), ("Reviews", 65, "#7BE8E0"), ("Delivery", 55, "#E9D67B"), ("Compatibility", 70, "#5EE9A8"), ("Durability", 74, "#F0A36A"), ("Seller risk", 90, "#7BE8E0"))
            : BuildCriteria(("Preço", 82, "#7BE8E0"), ("Garantia", 78, "#5EE9A8"), ("Autenticidade", 88, "#B49CFF"), ("Avaliações", 65, "#7BE8E0"), ("Entrega", 55, "#E9D67B"), ("Compatibilidade", 70, "#5EE9A8"), ("Durabilidade", 74, "#F0A36A"), ("Risco do vendedor", 90, "#7BE8E0"));
    }

    private static IReadOnlyList<CriteriaWeight> BuildCriteria(params (string Name, int Value, string Accent)[] criteria)
    {
        return criteria.Select(item => new CriteriaWeight(item.Name, item.Value, item.Accent)).ToList();
    }

    private static bool ContainsAnyTerm(string normalizedQuery, params string[] terms)
    {
        return terms.Any(term => ContainsCatalogTerm(normalizedQuery, NormalizeCatalogText(term)));
    }

    private static bool IsUnsupportedKnownCategory(string normalizedQuery)
    {
        return ContainsAnyTerm(normalizedQuery, "mesa", "mesas", "desk", "desks", "secretaria", "escrivaninha")
            || ContainsAnyTerm(normalizedQuery, "microfone", "microfones", "microphone", "microphones");
    }

    private static bool MatchesKnownProduct(string normalizedQuery, string rawValue, IReadOnlyList<ProductResult> products)
    {
        return products.Any(product =>
        {
            return QueryMentionsKnownProduct(normalizedQuery, rawValue, product);
        });
    }

    private static bool TryFindKnownProductMatches(
        IReadOnlyList<ProductResult> products,
        string rawValue,
        out IReadOnlyList<ProductResult> matches)
    {
        var normalizedQuery = NormalizeCatalogText(rawValue);
        matches = products
            .Where(product => QueryMentionsKnownProduct(normalizedQuery, rawValue, product))
            .OrderByDescending(product => ScoreProduct(product, normalizedQuery, BuildSearchTerms(normalizedQuery)))
            .ThenByDescending(product => product.Score)
            .ToList();

        return matches.Count > 0;
    }

    private static bool QueryMentionsKnownProduct(string normalizedQuery, string rawValue, ProductResult product)
    {
        var normalizedName = NormalizeCatalogText(product.Name);
        return product.Slug.Equals(rawValue, StringComparison.OrdinalIgnoreCase)
            || normalizedQuery.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
            || (normalizedQuery.Length > 4 && normalizedName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static int ScoreCatalog(string normalizedQuery, IReadOnlyList<string> terms)
    {
        return terms.Count(term => ContainsCatalogTerm(normalizedQuery, term));
    }

    private static bool ContainsCatalogTerm(string normalizedQuery, string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(normalizedTerm))
        {
            return false;
        }

        if (normalizedTerm.Any(character => !char.IsLetterOrDigit(character)))
        {
            return normalizedQuery.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
        }

        return GetCatalogTokens(normalizedQuery).Contains(normalizedTerm, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetCatalogTokens(string normalizedQuery)
    {
        return normalizedQuery
            .Split([' ', '-', '/', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\''], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeCatalogText(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private ProductSourceLink BuildOfficialSource(ProductResult product)
    {
        var url = product.Slug switch
        {
            "iphone-17" => "https://www.apple.com/iphone-17/specs/",
            "iphone-17-pro" => "https://www.apple.com/iphone-17-pro/specs/",
            "getac-ux10-g3" => "https://www.getac.com/pt/products/tablets/ux10/",
            "panasonic-toughbook-g2" => "https://eu.connect.panasonic.com/pt/pt/toughbook/toughbook-g2-series/toughbook-g2-mk3-standard",
            "zebra-et45-10" => "https://www.zebra.com/us/en/products/tablets/et4x-series/et45.html",
            "samsung-galaxy-tab-active4-pro" => "https://www.samsungmobilepress.com/press-releases/introducing-the-galaxy-tab-active4-pro-a-ruggedized-device-designed-for-the-new-mobile-workforce",
            "bosch-serie-6-frigorifico" => "https://www.bosch-home.pt/catalogo/frigorificos-congeladores/frigorificos-com-congelador/combinados-de-instalacao-livre/KGN39AIAT",
            "logitech-g305-lightspeed" => "https://www.logitech.com/en-eu/shop/p/g305-lightspeed-wireless-gaming-mouse",
            "logitech-m650-signature" => "https://www.logitech.com/en-eu/shop/p/m650-signature-wireless-mouse",
            "logitech-mx-keys-s" => "https://www.logitech.com/en-eu/shop/p/mx-keys-s",
            "logitech-k380" => "https://www.logitech.com/en-eu/search?q=Logitech%20K380",
            "razer-deathadder-essential" => "https://www.razer.com/gaming-mice/razer-deathadder-essential",
            _ => BuildOfficialBrandSearchUrl(product)
        };

        return new ProductSourceLink(
            text.Pick($"{product.Brand} - página oficial", $"{product.Brand} - official page"),
            url,
            text.Pick(
                "Usa a página da marca como fonte principal para validar especificações antes da compra.",
                "Use the brand page as the primary source to validate specifications before buying."));
    }

    private IReadOnlyList<ProductSpecification> BuildOfficialSpecifications(ProductResult product)
    {
        var specifications = product.Slug switch
        {
            "iphone-17" => new[]
            {
                new ProductSpecification(text.Pick("Ecra", "Display"), "6.3\" Super Retina XDR OLED, ProMotion ate 120Hz"),
                new ProductSpecification(text.Pick("Chip", "Chip"), "A19"),
                new ProductSpecification(text.Pick("Camara", "Camera"), "48MP Dual Fusion"),
                new ProductSpecification(text.Pick("Bateria", "Battery"), text.Pick("Ate 30h de reproducao de video", "Up to 30h video playback")),
                new ProductSpecification(text.Pick("Capacidade", "Capacity"), "256GB / 512GB")
            },
            "iphone-17-pro" => new[]
            {
                new ProductSpecification(text.Pick("Ecra", "Display"), "6.3\" Super Retina XDR OLED, ProMotion ate 120Hz"),
                new ProductSpecification(text.Pick("Chip", "Chip"), "A19 Pro"),
                new ProductSpecification(text.Pick("Camara", "Camera"), "48MP Pro Fusion, teleobjetiva 4x"),
                new ProductSpecification(text.Pick("Bateria", "Battery"), text.Pick("Ate 33h de reproducao de video", "Up to 33h video playback")),
                new ProductSpecification(text.Pick("Capacidade", "Capacity"), "256GB / 512GB / 1TB")
            },
            "getac-ux10-g3" => new[]
            {
                new ProductSpecification(text.Pick("Ecrã", "Display"), "10.1\" FHD LumiBond, 1000 nits"),
                new ProductSpecification(text.Pick("Sistema", "OS"), "Windows 11 Pro"),
                new ProductSpecification(text.Pick("Resistência", "Rugged rating"), "IP66, MIL-STD-810H"),
                new ProductSpecification(text.Pick("Toque", "Touch"), text.Pick("Capacitivo, luvas e chuva", "Capacitive, glove and rain mode")),
                new ProductSpecification(text.Pick("Uso recomendado", "Recommended use"), text.Pick("Chão de fábrica, manutenção e campo", "Factory floor, maintenance and field work"))
            },
            "bosch-serie-6-frigorifico" => new[]
            {
                new ProductSpecification(text.Pick("Modelo", "Model"), "KGN39AIAT"),
                new ProductSpecification(text.Pick("Eficiência", "Efficiency"), "Classe A"),
                new ProductSpecification(text.Pick("Capacidade", "Capacity"), "260 L frio + 103 L congelação"),
                new ProductSpecification(text.Pick("Ruído", "Noise"), "29 dB"),
                new ProductSpecification(text.Pick("Dimensões", "Dimensions"), "203 x 60 cm")
            },
            "panasonic-toughbook-g2" => new[]
            {
                new ProductSpecification(text.Pick("Ecrã", "Display"), "10.1\" WUXGA, 1000 nits"),
                new ProductSpecification(text.Pick("Sistema", "OS"), "Windows 11 Pro"),
                new ProductSpecification(text.Pick("Resistência", "Rugged rating"), "IP65, MIL-STD-810H"),
                new ProductSpecification(text.Pick("Módulos", "Modules"), "xPAK, teclado destacável, LTE/5G opcional"),
                new ProductSpecification(text.Pick("Uso recomendado", "Recommended use"), text.Pick("Ambiente industrial exigente", "Demanding industrial environments"))
            },
            "zebra-et45-10" => new[]
            {
                new ProductSpecification(text.Pick("Ecrã", "Display"), "10.1\" FHD"),
                new ProductSpecification(text.Pick("Sistema", "OS"), "Android Enterprise"),
                new ProductSpecification(text.Pick("Conectividade", "Connectivity"), "Wi-Fi 6, 5G opcional"),
                new ProductSpecification(text.Pick("Bateria", "Battery"), text.Pick("Até 10h na versão 10\"", "Up to 10h on the 10\" version")),
                new ProductSpecification(text.Pick("Uso recomendado", "Recommended use"), text.Pick("Logística, inventário e operações móveis", "Logistics, inventory and mobile operations"))
            },
            "samsung-galaxy-tab-active4-pro" => new[]
            {
                new ProductSpecification(text.Pick("Ecrã", "Display"), "10.1\" WUXGA"),
                new ProductSpecification(text.Pick("Sistema", "OS"), "Android"),
                new ProductSpecification(text.Pick("Resistência", "Rugged rating"), "IP68, MIL-STD-810H"),
                new ProductSpecification(text.Pick("Interação", "Interaction"), "S Pen IP68, toque com luvas"),
                new ProductSpecification(text.Pick("Uso recomendado", "Recommended use"), text.Pick("Equipas móveis com apps Android/web", "Mobile teams with Android/web apps"))
            },
            _ => product.Specs
                .Take(6)
                .Select((specification, index) => new ProductSpecification(text.Pick($"Spec {index + 1}", $"Spec {index + 1}"), specification))
                .ToArray()
        };

        return specifications;
    }

    private IReadOnlyList<ProductReviewLink> BuildReviewLinks(ProductResult product, string? query)
    {
        var reviewQuery = string.IsNullOrWhiteSpace(query)
            ? product.Name
            : $"{product.Name} {query}";

        var secondaryReviewQuery = $"{reviewQuery} {BuildYouTubeReviewQualifier(product, query)}";

        return new[]
        {
            new ProductReviewLink(
                text.Pick($"Review YouTube mais visto - {product.Name}", $"Most-viewed YouTube review - {product.Name}"),
                "YouTube",
                BuildYouTubeReviewSearchUrl($"{product.Name} review"),
                text.Pick("Pesquisa ordenada por visualizações", "Search sorted by view count")),
            new ProductReviewLink(
                text.Pick($"Teste em uso real - {product.Name}", $"Real-world test - {product.Name}"),
                "YouTube",
                BuildYouTubeReviewSearchUrl(secondaryReviewQuery),
                text.Pick("Confirmar visualizações no YouTube", "Confirm views on YouTube"))
        };
    }

    private static string BuildYouTubeReviewSearchUrl(string query)
    {
        return $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}&sp=CAM%253D";
    }

    private static string BuildYouTubeReviewQualifier(ProductResult product, string? query)
    {
        var normalizedQuery = NormalizeCatalogText(query ?? string.Empty);

        if (ProductContainsAny(product, "rugged", "toughbook", "getac", "zebra", "tablet robusto")
            || ContainsAnyTerm(normalizedQuery, "rugged", "robusto", "todoterreno", "chao de fabrica", "fabrica"))
        {
            return "rugged tablet field test";
        }

        if (ProductContainsAny(product, "disco externo", "ssd externo", "hdd externo", "western digital", "seagate", "samsung t7"))
        {
            return "review benchmark";
        }

        if (ProductContainsAny(product, "teclado", "keyboard", "keychron", "logitech", "mx keys"))
        {
            return "review typing test";
        }

        if (ProductContainsAny(product, "carregador", "charger", "usb-c", "magsafe", "power delivery"))
        {
            return "review carregador iphone teste seguranca";
        }

        if (ProductContainsAny(product, "impressora", "printer", "multifuncoes", "multifunction"))
        {
            return "review teste impressao";
        }

        if (ProductContainsAny(product, "fralda", "fraldas", "pampers", "dodot", "bebe"))
        {
            return "review opiniao bebe";
        }

        if (ProductContainsAny(product, "monitor", "oled", "4k", "144hz"))
        {
            return "review teste imagem";
        }

        if (ProductContainsAny(product, "auscultadores", "headphones", "earbuds", "noise cancelling"))
        {
            return "review sound test";
        }

        return "review teste";
    }

    private static string BuildOfficialBrandSearchUrl(ProductResult product)
    {
        var query = Uri.EscapeDataString(product.Name);
        return product.Brand.ToLowerInvariant() switch
        {
            "apple" => $"https://www.apple.com/pt/search/{query}",
            "samsung" => $"https://www.samsung.com/pt/search/?searchvalue={query}",
            "lenovo" => $"https://www.lenovo.com/pt/pt/search?text={query}",
            "dell" => $"https://www.dell.com/pt-pt/search/{query}",
            "asus" => $"https://www.asus.com/pt/searchresult?searchType=products&searchKey={query}",
            "google" => $"https://store.google.com/search?q={query}",
            "xiaomi" => $"https://www.mi.com/pt/search/{query}",
            "lg" => $"https://www.lg.com/pt/search/?search={query}",
            "miele" => $"https://www.miele.pt/search/?q={query}",
            "logitech" => $"https://www.logitech.com/en-eu/search?q={query}",
            "microsoft" => $"https://www.microsoft.com/pt-pt/search?q={query}",
            "razer" => $"https://www.razer.com/search/{query}",
            "anker" => $"https://www.anker.com/eu-en/search?q={query}",
            "ugreen" => $"https://eu.ugreen.com/search?q={query}",
            "belkin" => $"https://www.belkin.com/search/?q={query}",
            "getac" => $"https://www.getac.com/pt/search/?q={query}",
            "panasonic" => $"https://eu.connect.panasonic.com/pt/pt/search?search={query}",
            "zebra" => $"https://www.zebra.com/us/en/search.html?q={query}",
            "western digital" => $"https://www.westerndigital.com/search?q={query}",
            "seagate" => $"https://www.seagate.com/search/?keyword={query}",
            "keychron" => $"https://www.keychron.com/search?q={query}",
            "sony" => $"https://www.sony.pt/search?query={query}",
            "jbl" => $"https://www.jbl.com/search?q={query}",
            "tcl" => $"https://www.tcl.com/global/en/search?keyword={query}",
            "de'longhi" => $"https://www.delonghi.com/pt-pt/search?q={query}",
            "nespresso" => $"https://www.nespresso.com/pt/pt/search?q={query}",
            "sage" => $"https://www.sageappliances.com/eu/pt/search.html?q={query}",
            "bosch" => $"https://www.bosch-professional.com/pt/pt/search/?text={query}",
            "makita" => $"https://www.makita.pt/search/?q={query}",
            "dewalt" => $"https://www.dewalt.pt/search?query={query}",
            "ikea" => $"https://www.ikea.com/pt/pt/search/?q={query}",
            "sihoo" => $"https://www.sihoooffice.com/search?q={query}",
            "songmics" => $"https://www.songmics.com/search?q={query}",
            "michelin" => $"https://www.michelin.pt/auto/search?q={query}",
            "continental" => $"https://www.continental-tires.com/pt/pt/search/?query={query}",
            "bridgestone" => $"https://www.bridgestone.pt/search?q={query}",
            "hp" => $"https://www.hp.com/pt-pt/shop/list.aspx?search={query}",
            "epson" => $"https://www.epson.pt/pt_PT/search?q={query}",
            "brother" => $"https://www.brother.pt/search?keyword={query}",
            "royal canin" => $"https://www.royalcanin.com/pt/search?q={query}",
            "purina" => $"https://www.purina.pt/search?search={query}",
            "libra" => $"https://www.google.com/search?q={Uri.EscapeDataString(product.Brand + " " + product.Name + " official")}",
            "pampers" => $"https://www.pampers.pt/search?q={query}",
            "dodot" => $"https://www.dodot.pt/search?q={query}",
            "chicco" => $"https://www.chicco.pt/search?q={query}",
            _ => $"https://www.google.com/search?q={Uri.EscapeDataString(product.Brand + " " + product.Name + " official specifications")}"
        };
    }

    private IReadOnlyList<ProductResult> LocalizeProducts(IReadOnlyList<ProductResult> products)
    {
        return text.IsEnglish ? products.Select(LocalizeProduct).ToList() : products;
    }

    private ProductResult LocalizeProduct(ProductResult product)
    {
        if (!text.IsEnglish)
        {
            return product;
        }

        return product with
        {
            Badge = TranslateProductCopy(product.Badge),
            Specs = product.Specs.Select(TranslateProductCopy).ToList(),
            Highlights = product.Highlights.Select(TranslateProductCopy).ToList(),
            VerificationChecks = product.VerificationChecks.Select(TranslateProductCopy).ToList(),
            FraudAlerts = product.FraudAlerts.Select(alert => alert with { Reason = TranslateProductCopy(alert.Reason) }).ToList(),
            AiSummary = TranslateProductCopy(product.AiSummary)
        };
    }

    private IReadOnlyList<SellerOffer> LocalizeOffers(IReadOnlyList<SellerOffer> offers)
    {
        return text.IsEnglish
            ? offers.Select(offer => offer with
            {
                Delivery = TranslateProductCopy(offer.Delivery),
                Warranty = TranslateProductCopy(offer.Warranty),
                Status = TranslateProductCopy(offer.Status)
            }).ToList()
            : offers;
    }

    private static string TranslateProductCopy(string value)
    {
        return value switch
        {
            "Recomendado" => "Recommended",
            "Melhor garantia" => "Best warranty",
            "Ecrã premium" => "Premium display",
            "Melhor preço" => "Best price",
            "Melhor Android" => "Best Android",
            "Melhor câmara IA" => "Best AI camera",
            "Melhor preço/performance" => "Best price/performance",
            "Mais eficiente" => "Most efficient",
            "Melhor tecnologia" => "Best technology",
            "Melhor durabilidade" => "Best durability",
            "Melhor preco" => "Best price",
            "18h autonomia" => "18h battery",
            "15h autonomia" => "15h battery",
            "12h autonomia" => "12h battery",
            "11h autonomia" => "11h battery",
            "23h vídeo" => "23h video",
            "Garantia 2 anos" => "2-year warranty",
            "Garantia 3 anos" => "3-year warranty",
            "Autonomia 38% acima da média" => "Battery life 38% above average",
            "Peso reduzido" => "Low weight",
            "Garantia oficial em Portugal" => "Official warranty in Portugal",
            "Mais leve da lista" => "Lightest on the list",
            "Garantia superior" => "Stronger warranty",
            "Bom equilíbrio profissional" => "Good professional balance",
            "Boa performance" => "Good performance",
            "Ecrã forte" => "Strong display",
            "Construção sólida" => "Solid build",
            "Preço competitivo" => "Competitive price",
            "Leve" => "Light",
            "Boa relação valor" => "Good value",
            "Melhor ecossistema" => "Best ecosystem",
            "Câmara consistente" => "Consistent camera",
            "Suporte longo" => "Long support",
            "Ecrã excelente" => "Excellent display",
            "Atualizações longas" => "Long update window",
            "Boa autonomia" => "Good battery",
            "Fotografia forte" => "Strong photography",
            "Android limpo" => "Clean Android",
            "Excelente valor" => "Excellent value",
            "Performance elevada" => "High performance",
            "Carregamento rápido" => "Fast charging",
            "Preço agressivo" => "Aggressive price",
            "Número de série validado" => "Serial number validated",
            "Consumo muito baixo" => "Very low consumption",
            "Silencioso" => "Quiet",
            "Boa capacidade familiar" => "Good family capacity",
            "Capacidade superior" => "Higher capacity",
            "Arrefecimento rápido" => "Fast cooling",
            "Arrefecimento rapido" => "Fast cooling",
            "Funcionalidades smart" => "Smart features",
            "Construção robusta" => "Robust build",
            "Construcao robusta" => "Robust build",
            "Dosagem automática" => "Automatic dosing",
            "Dosagem automatica" => "Automatic dosing",
            "Lavagem silenciosa" => "Quiet washing",
            "Baixo ruído" => "Low noise",
            "Baixo ruido" => "Low noise",
            "Programas inteligentes" => "Smart programs",
            "Numero de serie validado" => "Serial number validated",
            "Revendedor autorizado" => "Authorized reseller",
            "Stock real confirmado" => "Real stock confirmed",
            "Garantia oficial registável em PT" => "Official warranty registerable in Portugal",
            "Sem histórico de contrafação" => "No counterfeit history",
            "Garantia validada" => "Warranty validated",
            "Etiqueta energética confirmada" => "Energy label confirmed",
            "Etiqueta energetica confirmada" => "Energy label confirmed",
            "Stock cruzado em 3 fontes" => "Stock cross-checked across 3 sources",
            "Sem alertas críticos" => "No critical alerts",
            "Sem alertas criticos" => "No critical alerts",
            "Vendedor autorizado" => "Authorized seller",
            "Preço dentro do mercado" => "Market-aligned price",
            "IMEI validável" => "IMEI can be validated",
            "Preco dentro do mercado" => "Market-aligned price",
            "Garantia UE confirmada" => "EU warranty confirmed",
            "Preço 60% abaixo do mercado" => "Price 60% below market",
            "Domínio criado há 14 dias" => "Domain created 14 days ago",
            "Sem registo fiscal verificável" => "No verifiable tax registration",
            "Preço 45% abaixo do mercado" => "Price 45% below market",
            "Sem informação fiscal verificável" => "No verifiable tax information",
            "Preço 50% abaixo do mercado" => "Price 50% below market",
            "Preco 50% abaixo do mercado" => "Price 50% below market",
            "Sem registo fiscal verificavel" => "No verifiable tax registration",
            "2 dias" => "2 days",
            "3 dias" => "3 days",
            "4 dias" => "4 days",
            "5-7 dias" => "5-7 days",
            "4-7 dias" => "4-7 days",
            "2-4 dias" => "2-4 days",
            "2-3 dias" => "2-3 days",
            "Confirmar loja" => "Confirm store",
            "2 anos oficial" => "2-year official",
            "3 anos UE" => "3-year EU",
            "3 anos PT" => "3-year PT",
            "3 anos oficial" => "3-year official",
            "Validar vendedor" => "Validate seller",
            "Verificado" => "Verified",
            "Autorizado" => "Authorized",
            "A comparar" => "Comparing",
            "Este produto cumpre 94% dos critérios definidos. Pontos fortes: autonomia, peso reduzido e garantia oficial." => "This product meets 94% of the selected criteria. Strengths: battery life, low weight and official warranty.",
            "Excelente opção para suporte e mobilidade. Fica atrás do primeiro lugar pelo preço mais alto." => "Excellent option for support and mobility. It sits behind first place because of the higher price.",
            "Boa escolha se o ecrã for prioritário. Penalizado pelo peso e autonomia abaixo dos líderes." => "Good choice if display quality is the priority. Penalized by weight and battery life below the leaders.",
            "A melhor opção para orçamento controlado. Menos indicado para workloads pesados." => "The best option for a controlled budget. Less suited to heavy workloads.",
            "Melhor escolha premium para quem valoriza câmara, desempenho e suporte prolongado." => "Best premium choice for camera, performance and long-term support.",
            "A opção Android mais equilibrada para desempenho, ecrã e longevidade." => "The most balanced Android option for performance, display and longevity.",
            "Ideal para fotografia computacional e experiência Android limpa." => "Ideal for computational photography and a clean Android experience.",
            "Muito forte em performance e carregamento, com preço competitivo." => "Very strong on performance and charging, with a competitive price.",
            "Melhor escolha para frigorífico eficiente, silencioso e com garantia forte." => "Best choice for an efficient, quiet fridge with a strong warranty.",
            "Melhor escolha para frigorifico eficiente, silencioso e com garantia forte." => "Best choice for an efficient, quiet fridge with a strong warranty.",
            "Excelente para famílias que precisam de grande capacidade e controlo inteligente." => "Excellent for families that need high capacity and smart control.",
            "Excelente para familias que precisam de grande capacidade e controlo inteligente." => "Excellent for families that need high capacity and smart control.",
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preço inicial." => "Good choice when durability and wash quality matter more than initial price.",
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preco inicial." => "Good choice when durability and wash quality matter more than initial price.",
            "Opção equilibrada para cozinha moderna com bom preço e baixo ruído." => "Balanced option for a modern kitchen with good price and low noise.",
            "Opcao equilibrada para cozinha moderna com bom preco e baixo ruido." => "Balanced option for a modern kitchen with good price and low noise.",
            _ => value
        };
    }

    private sealed record RetailerProfile(
        string Market,
        string LocationPt,
        string LocationEn,
        string Name,
        IReadOnlyList<string> Catalogs,
        IReadOnlyList<string> Brands,
        int ReliabilityScore,
        decimal PriceFactor,
        string DeliveryPt,
        string DeliveryEn,
        string WarrantyPt,
        string WarrantyEn,
        string StatusPt,
        string StatusEn,
        string UrlFormat,
        bool DirectSeller);

    private sealed record RetailerProductOffer(
        string Url,
        long PriceCents,
        string DeliveryPt,
        string DeliveryEn);
}
