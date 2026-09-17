/**
 * Technical Text Rendering Utilities
 * Ensures robust UTF-8 NFC normalization, safe character decoding,
 * and clean display for technical interview questions and code symbols.
 */

const ENTITY_MAP: Record<string, string> = {
  '&quot;': '"',
  '&#34;': '"',
  '&apos;': "'",
  '&#39;': "'",
  '&amp;': '&',
  '&#38;': '&',
  '&lt;': '<',
  '&#60;': '<',
  '&gt;': '>',
  '&#62;': '>',
  '&nbsp;': ' ',
  '&#160;': ' ',
  '&mdash;': '—',
  '&#8212;': '—',
  '&ndash;': '–',
  '&#8211;': '–',
  '&lsquo;': '‘',
  '&#8216;': '‘',
  '&rsquo;': '’',
  '&#8217;': '’',
  '&ldquo;': '“',
  '&#8220;': '“',
  '&rdquo;': '”',
  '&#8221;': '”'
};

const MOJIBAKE_MAP: [RegExp, string][] = [
  [/â€™/g, "'"],
  [/â€˜/g, "'"],
  [/â€œ/g, '"'],
  [/â€/g, '"'],
  [/â€”/g, '—'],
  [/â€“/g, '–'],
  [/Â /g, ' '],
  [/Ã©/g, 'é'],
  [/Ã¨/g, 'è'],
  [/Ã /g, 'à']
];

export function normalizeTechnicalText(text: string | null | undefined): string {
  if (!text) return '';

  // 1. Unicode NFC normalization
  let normalized = text.normalize('NFC');

  // 2. Fix mojibake artifacts from encoding mismatches
  for (const [pattern, replacement] of MOJIBAKE_MAP) {
    normalized = normalized.replace(pattern, replacement);
  }

  // 3. Safe entity decoding (decode specific character entities without innerHTML/XSS risk)
  normalized = normalized.replace(
    /&(?:quot|#34|apos|#39|amp|#38|lt|#60|gt|#62|nbsp|#160|mdash|#8212|ndash|#8211|lsquo|#8216|rsquo|#8217|ldquo|#8220|rdquo|#8221);/gi,
    match => ENTITY_MAP[match.toLowerCase()] || match
  );

  return normalized;
}
