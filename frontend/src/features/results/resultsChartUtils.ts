import type { ResultsCategoryGroup, ResultsCategorySummary, ResultsQuestionHighlight } from '../../types';

export const RATING_MAX = 5;

export const SERIES_COLORS = {
  Self: '#0875E1',
  Peer: '#FF9800',
  Manager: '#2E7D32',
  '360': '#E65100',
} as const;

export const RADAR_SERIES_IDS = ['self', 'peer', 'manager', '360'] as const;
export type RadarSeriesId = (typeof RADAR_SERIES_IDS)[number];

export type RadarSeriesSummary = {
  id: RadarSeriesId;
  label: string;
  color: string;
  data: number[];
  nullMask: boolean[];
};

export function formatAverage(value: number | null | undefined): string {
  if (value == null) return '—';
  return value.toFixed(1);
}

export const MAX_LABEL_LINES = 3;

const MIN_CHARS_PER_LINE = 24;
const CHAR_WIDTH_PX = 6.5;
const BAR_ROW_BAND_HEIGHT = 44;
const CHART_VERTICAL_MARGIN = 72;

function wrapWordsToLines(text: string, maxCharsPerLine: number): string[] {
  const words = text.split(/\s+/).filter(Boolean);
  if (words.length === 0) return [text];

  const lines: string[] = [];
  let current = '';

  for (const word of words) {
    const candidate = current ? `${current} ${word}` : word;
    if (candidate.length <= maxCharsPerLine) {
      current = candidate;
      continue;
    }
    if (current) lines.push(current);
    if (word.length <= maxCharsPerLine) {
      current = word;
    } else {
      let remaining = word;
      while (remaining.length > maxCharsPerLine) {
        lines.push(remaining.slice(0, maxCharsPerLine));
        remaining = remaining.slice(maxCharsPerLine);
      }
      current = remaining;
    }
  }
  if (current) lines.push(current);
  return lines;
}

export function wrapQuestionLabel(text: string, maxLines = MAX_LABEL_LINES): string {
  if (!text.trim()) return text;

  let charsPerLine = Math.max(MIN_CHARS_PER_LINE, Math.ceil(text.length / maxLines));
  let lines = wrapWordsToLines(text, charsPerLine);

  while (lines.length > maxLines) {
    charsPerLine += 4;
    lines = wrapWordsToLines(text, charsPerLine);
  }

  return lines.join('\n');
}

export function countWrappedLines(text: string, maxLines = MAX_LABEL_LINES): number {
  return wrapQuestionLabel(text, maxLines).split('\n').length;
}

export function estimateYAxisWidth(labels: string[]): number {
  const longestLine = labels.reduce((max, label) => {
    const lineLengths = label.split('\n').map((line) => line.length);
    return Math.max(max, ...lineLengths, 0);
  }, 0);
  return Math.max(120, Math.ceil(longestLine * CHAR_WIDTH_PX) + 24);
}

export function estimateBarChartHeight(rowCount: number, maxLines: number): number {
  const labelPadding = Math.max(0, (maxLines - 1) * 6);
  return CHART_VERTICAL_MARGIN + rowCount * BAR_ROW_BAND_HEIGHT + labelPadding;
}

export function maxWrappedLines(labels: string[]): number {
  return labels.reduce((max, label) => Math.max(max, label.split('\n').length), 1);
}

export function buildBarChartDataset(group: ResultsCategoryGroup) {
  return group.labels.map((label, i) => ({
    question: wrapQuestionLabel(label),
    Self: group.series.find((s) => s.name === 'Self')?.data[i] ?? 0,
    Peer: group.series.find((s) => s.name === 'Peer')?.data[i] ?? 0,
    Manager: group.series.find((s) => s.name === 'Manager')?.data[i] ?? 0,
  }));
}

export function buildHighlightBarDataset(questions: ResultsQuestionHighlight[]) {
  return questions.map((q) => ({
    question: wrapQuestionLabel(q.questionText),
    Peer: q.peerAverage ?? 0,
    Manager: q.managerAverage ?? 0,
  }));
}

export function summariesForRadar(summaries: ResultsCategorySummary[]) {
  const rated = summaries.filter(
    (s) => s.selfAverage != null || s.peerAverage != null || s.managerAverage != null || s.overallAverage != null,
  );

  const metrics = rated.map((s) => s.categoryName);
  const nullMask = {
    self: rated.map((s) => s.selfAverage == null),
    peer: rated.map((s) => s.peerAverage == null),
    manager: rated.map((s) => s.managerAverage == null),
    overall: rated.map((s) => s.overallAverage == null),
  };

  const series: RadarSeriesSummary[] = [
    {
      id: 'self',
      label: 'Self',
      color: SERIES_COLORS.Self,
      data: rated.map((s) => s.selfAverage ?? 0),
      nullMask: nullMask.self,
    },
    {
      id: 'peer',
      label: 'Peer',
      color: SERIES_COLORS.Peer,
      data: rated.map((s) => s.peerAverage ?? 0),
      nullMask: nullMask.peer,
    },
    {
      id: 'manager',
      label: 'Manager',
      color: SERIES_COLORS.Manager,
      data: rated.map((s) => s.managerAverage ?? 0),
      nullMask: nullMask.manager,
    },
    {
      id: '360',
      label: '360',
      color: SERIES_COLORS['360'],
      data: rated.map((s) => s.overallAverage ?? 0),
      nullMask: nullMask.overall,
    },
  ];

  return { metrics, series, rated };
}
