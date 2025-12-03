const http = require('http');
const url = require('url');

const PORT = process.env.PORT || 3001;

// Halo 2 emblem generation service
// URL format: /P{primary}-S{secondary}-EP{tertiary}-ES{quaternary}-EF{fg}-EB{bg}-ET{toggle}.png

const server = http.createServer(async (req, res) => {
    // Enable CORS
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    if (req.method === 'OPTIONS') {
        res.writeHead(200);
        res.end();
        return;
    }

    if (req.method !== 'GET') {
        res.writeHead(405, { 'Content-Type': 'text/plain' });
        res.end('Method not allowed');
        return;
    }

    const parsedUrl = url.parse(req.url, true);
    const pathname = parsedUrl.pathname;

    // Health check endpoint
    if (pathname === '/health') {
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ status: 'ok', service: 'h2emblem' }));
        return;
    }

    // Parse the emblem URL format: /P{pri}-S{sec}-EP{ter}-ES{qua}-EF{fg}-EB{bg}-ET{toggle}.png
    // Example: /Pred-Sblue-EPwhite-ESblack-EF1-EB2-ET0.png
    const emblemMatch = pathname.match(/^\/P([^-]+)-S([^-]+)-EP([^-]+)-ES([^-]+)-EF(\d+)-EB(\d+)-ET(\d+)\.png$/i);

    if (!emblemMatch) {
        // Try query string format as fallback
        const query = parsedUrl.query;
        if (query.emblem || query.fg || query.bg) {
            return handleQueryFormat(query, res);
        }

        res.writeHead(400, { 'Content-Type': 'text/plain' });
        res.end('Invalid emblem URL format. Expected: /P{primary}-S{secondary}-EP{tertiary}-ES{quaternary}-EF{fg}-EB{bg}-ET{toggle}.png');
        return;
    }

    const [, primaryColor, secondaryColor, tertiaryColor, quaternaryColor, fg, bg, toggle] = emblemMatch;

    try {
        const emblemSvg = generateEmblemSvg({
            foreground: parseInt(fg) || 0,
            background: parseInt(bg) || 0,
            toggle: parseInt(toggle) || 0,
            primaryColor: primaryColor,      // Armor primary (background fill)
            secondaryColor: secondaryColor,  // Armor secondary
            tertiaryColor: tertiaryColor,    // Emblem primary (foreground)
            quaternaryColor: quaternaryColor // Emblem secondary (background shape)
        });

        res.writeHead(200, {
            'Content-Type': 'image/svg+xml',
            'Cache-Control': 'public, max-age=86400'
        });
        res.end(emblemSvg);
    } catch (error) {
        console.error('Error generating emblem:', error);
        res.writeHead(500, { 'Content-Type': 'text/plain' });
        res.end('Error generating emblem');
    }
});

function handleQueryFormat(query, res) {
    const { fg, bg, emblem, toggle, pri, sec, ter, qua } = query;

    try {
        const emblemSvg = generateEmblemSvg({
            foreground: parseInt(fg || emblem) || 0,
            background: parseInt(bg) || 0,
            toggle: parseInt(toggle) || 0,
            primaryColor: pri || 'red',
            secondaryColor: sec || 'blue',
            tertiaryColor: ter || 'white',
            quaternaryColor: qua || 'black'
        });

        res.writeHead(200, {
            'Content-Type': 'image/svg+xml',
            'Cache-Control': 'public, max-age=86400'
        });
        res.end(emblemSvg);
    } catch (error) {
        console.error('Error generating emblem:', error);
        res.writeHead(500, { 'Content-Type': 'text/plain' });
        res.end('Error generating emblem');
    }
}

// Halo 2 color palette (by index and name)
const H2_COLORS_BY_INDEX = [
    '#6E6E6E', // 0 - Steel
    '#C0C0C0', // 1 - Silver
    '#FFFFFF', // 2 - White
    '#B22222', // 3 - Red
    '#8B4789', // 4 - Mauve
    '#FA8072', // 5 - Salmon
    '#FF8C00', // 6 - Orange
    '#FF7F50', // 7 - Coral
    '#FFDAB9', // 8 - Peach
    '#FFD700', // 9 - Gold
    '#FFFF00', // 10 - Yellow
    '#FFFFE0', // 11 - Pale
    '#9ACD32', // 12 - Sage
    '#228B22', // 13 - Green
    '#6B8E23', // 14 - Olive
    '#008080', // 15 - Teal
    '#00CED1', // 16 - Aqua
    '#00FFFF', // 17 - Cyan
    '#0000CD', // 18 - Blue
    '#0047AB', // 19 - Cobalt
    '#082567', // 20 - Sapphire
    '#8A2BE2', // 21 - Violet
    '#DA70D6', // 22 - Orchid
    '#E6E6FA', // 23 - Lavender
    '#8B4513', // 24 - Brown
    '#D2B48C', // 25 - Tan
    '#F0E68C', // 26 - Khaki
    '#000000', // 27 - Black
];

const H2_COLORS_BY_NAME = {
    'steel': '#6E6E6E',
    'silver': '#C0C0C0',
    'white': '#FFFFFF',
    'red': '#B22222',
    'mauve': '#8B4789',
    'salmon': '#FA8072',
    'orange': '#FF8C00',
    'coral': '#FF7F50',
    'peach': '#FFDAB9',
    'gold': '#FFD700',
    'yellow': '#FFFF00',
    'pale': '#FFFFE0',
    'sage': '#9ACD32',
    'green': '#228B22',
    'olive': '#6B8E23',
    'teal': '#008080',
    'aqua': '#00CED1',
    'cyan': '#00FFFF',
    'blue': '#0000CD',
    'cobalt': '#0047AB',
    'sapphire': '#082567',
    'violet': '#8A2BE2',
    'orchid': '#DA70D6',
    'lavender': '#E6E6FA',
    'brown': '#8B4513',
    'tan': '#D2B48C',
    'khaki': '#F0E68C',
    'black': '#000000'
};

function getColor(colorValue) {
    if (!colorValue) return '#808080';

    // Check if it's a number (index)
    const index = parseInt(colorValue);
    if (!isNaN(index) && index >= 0 && index < H2_COLORS_BY_INDEX.length) {
        return H2_COLORS_BY_INDEX[index];
    }

    // Check if it's a color name
    const lower = String(colorValue).toLowerCase();
    if (H2_COLORS_BY_NAME[lower]) {
        return H2_COLORS_BY_NAME[lower];
    }

    // Return as-is if it looks like a hex color
    if (colorValue.startsWith('#')) {
        return colorValue;
    }

    return '#808080'; // Default gray
}

function generateEmblemSvg(params) {
    const {
        foreground,
        background,
        toggle,
        primaryColor,    // Armor primary
        secondaryColor,  // Armor secondary
        tertiaryColor,   // Emblem primary (foreground color)
        quaternaryColor  // Emblem secondary (background shape color)
    } = params;

    // Get actual colors
    const emblemFgColor = getColor(tertiaryColor);   // Foreground shape color
    const emblemBgColor = getColor(quaternaryColor); // Background shape color
    const armorPrimary = getColor(primaryColor);
    const armorSecondary = getColor(secondaryColor);

    // Background shapes (EB parameter)
    const bgShapes = [
        '', // 0 - none
        `<circle cx="32" cy="32" r="28" fill="${emblemBgColor}"/>`, // 1 - circle
        `<rect x="4" y="4" width="56" height="56" fill="${emblemBgColor}"/>`, // 2 - square
        `<polygon points="32,4 60,60 4,60" fill="${emblemBgColor}"/>`, // 3 - triangle up
        `<polygon points="32,60 60,4 4,4" fill="${emblemBgColor}"/>`, // 4 - triangle down
        `<polygon points="32,4 60,32 32,60 4,32" fill="${emblemBgColor}"/>`, // 5 - diamond
        `<polygon points="32,4 42,26 60,26 46,40 52,60 32,48 12,60 18,40 4,26 22,26" fill="${emblemBgColor}"/>`, // 6 - star
        `<ellipse cx="32" cy="32" rx="28" ry="18" fill="${emblemBgColor}"/>`, // 7 - oval horizontal
        `<ellipse cx="32" cy="32" rx="18" ry="28" fill="${emblemBgColor}"/>`, // 8 - oval vertical
        `<rect x="4" y="12" width="56" height="40" rx="6" fill="${emblemBgColor}"/>`, // 9 - rounded rect
        `<path d="M32 4 L56 16 L56 48 L32 60 L8 48 L8 16 Z" fill="${emblemBgColor}"/>`, // 10 - hexagon
        `<path d="M32 4 Q60 4 60 32 Q60 60 32 60 Q4 60 4 32 Q4 4 32 4" fill="${emblemBgColor}"/>`, // 11 - rounded square
    ];

    // Foreground shapes (EF parameter)
    const fgShapes = [
        '', // 0 - none
        `<polygon points="32,8 42,28 60,28 46,40 52,58 32,46 12,58 18,40 4,28 22,28" fill="${emblemFgColor}"/>`, // 1 - 5-point star
        `<circle cx="32" cy="32" r="18" fill="${emblemFgColor}"/>`, // 2 - circle
        `<rect x="14" y="14" width="36" height="36" fill="${emblemFgColor}"/>`, // 3 - square
        `<polygon points="32,10 54,54 10,54" fill="${emblemFgColor}"/>`, // 4 - triangle up
        `<polygon points="32,54 54,10 10,10" fill="${emblemFgColor}"/>`, // 5 - triangle down
        `<polygon points="32,8 56,32 32,56 8,32" fill="${emblemFgColor}"/>`, // 6 - diamond
        `<path d="M32 8 C52 8 56 32 56 32 C56 32 52 56 32 56 C12 56 8 32 8 32 C8 32 12 8 32 8" fill="${emblemFgColor}"/>`, // 7 - shield
        `<path d="M32 10 L42 18 L50 32 L42 46 L32 54 L22 46 L14 32 L22 18 Z" fill="${emblemFgColor}"/>`, // 8 - octagon
        `<path d="M26 14 L38 14 L38 26 L50 26 L50 38 L38 38 L38 50 L26 50 L26 38 L14 38 L14 26 L26 26 Z" fill="${emblemFgColor}"/>`, // 9 - cross/plus
        `<polygon points="32,6 36,22 52,22 40,32 44,48 32,40 20,48 24,32 12,22 28,22" fill="${emblemFgColor}"/>`, // 10 - star outline
        `<path d="M20 20 L44 20 L44 28 L36 28 L36 44 L28 44 L28 28 L20 28 Z" fill="${emblemFgColor}"/>`, // 11 - T shape
        `<text x="32" y="44" font-size="36" text-anchor="middle" fill="${emblemFgColor}" font-family="Arial Black" font-weight="bold">7</text>`, // 12 - number 7
        `<text x="32" y="44" font-size="36" text-anchor="middle" fill="${emblemFgColor}" font-family="Arial Black" font-weight="bold">X</text>`, // 13 - X
        `<path d="M16 16 L48 48 M48 16 L16 48" stroke="${emblemFgColor}" stroke-width="8" fill="none"/>`, // 14 - X lines
        `<path d="M32 12 L32 52 M12 32 L52 32" stroke="${emblemFgColor}" stroke-width="6" fill="none"/>`, // 15 - crosshairs
    ];

    const bgIndex = Math.min(Math.max(0, background), bgShapes.length - 1);
    const fgIndex = Math.min(Math.max(0, foreground), fgShapes.length - 1);

    // Background plate color (using armor primary as the plate background)
    const plateColor = armorPrimary;

    return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64">
  <rect width="64" height="64" fill="${plateColor}" rx="4"/>
  ${bgShapes[bgIndex]}
  ${fgShapes[fgIndex]}
</svg>`;
}

server.listen(PORT, '0.0.0.0', () => {
    console.log(`H2 Emblem Service running on port ${PORT}`);
    console.log(`Test URL: http://localhost:${PORT}/P3-S18-EP2-ES27-EF1-EB1-ET0.png`);
    console.log(`Health: http://localhost:${PORT}/health`);
});
