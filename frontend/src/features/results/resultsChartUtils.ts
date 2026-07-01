import type { ResultsCategoryGroup, ResultsCategorySummary } from '../../types';

export const RATING_MAX = 5;

export const SERIES_COLORS = {
  Self: '#0875E1',
  Peer: '#5E6A75',
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

export function buildBarChartDataset(group: ResultsCategoryGroup) {
  return group.labels.map((label, i) => ({
    question: label.length > 30 ? label.slice(0, 30) + '…' : label,
    Self: group.series.find((s) => s.name === 'Self')?.data[i] ?? 0,
    Peer: group.series.find((s) => s.name === 'Peer')?.data[i] ?? 0,
    Manager: group.series.find((s) => s.name === 'Manager')?.data[i] ?? 0,
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
