import React from 'react';
import { normalizeTechnicalText } from '../lib/text-utils';

interface TechnicalTextProps {
  text: string | null | undefined;
  className?: string;
  'data-testid'?: string;
}

/**
 * Parses inline markdown code blocks (`code`) and line breaks
 * into clean, beautifully styled React elements.
 */
export const TechnicalText: React.FC<TechnicalTextProps> = ({
  text,
  className,
  'data-testid': testId
}) => {
  if (!text) return null;

  const cleanText = normalizeTechnicalText(text);

  // Split by backticks: even index = plain text, odd index = code snippet
  const parts: string[] = cleanText.split(/(`[^`]+`)/g);

  return (
    <span className={className} data-testid={testId}>
      {parts.map((part: string, index: number) => {
        if (part.startsWith('`') && part.endsWith('`') && part.length >= 2) {
          const codeContent = part.slice(1, -1);
          return (
            <code
              key={index}
              className="px-1.5 py-0.5 mx-0.5 text-[0.9em] font-mono rounded bg-slate-800/90 text-cyan-300 border border-slate-700/80 inline-block align-baseline font-medium"
            >
              {codeContent}
            </code>
          );
        }
        return <React.Fragment key={index}>{part}</React.Fragment>;
      })}
    </span>
  );
};
