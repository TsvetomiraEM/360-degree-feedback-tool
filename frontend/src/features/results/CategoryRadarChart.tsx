import { useCallback, useMemo, useState } from 'react';
import { RadarChart } from '@mui/x-charts/RadarChart';
import type { VisibilityIdentifier } from '@mui/x-charts/plugins';
import { RATING_MAX, type RadarSeriesId, type RadarSeriesSummary } from './resultsChartUtils';

type CategoryRadarChartProps = {
  metrics: string[];
  series: RadarSeriesSummary[];
};

type RadarHiddenItem = {
  type: 'radar';
  seriesId: RadarSeriesId;
};

/** MUI series use `id`; visibility state uses `seriesId` — normalize callbacks to that shape. */
function toRadarHiddenItem(
  item: VisibilityIdentifier<'radar'>,
  knownSeriesIds: Set<RadarSeriesId>,
): RadarHiddenItem | null {
  const candidate = item.seriesId ?? (item as { id?: RadarSeriesId }).id;
  if (candidate == null || !knownSeriesIds.has(candidate as RadarSeriesId)) return null;
  return { type: 'radar', seriesId: candidate as RadarSeriesId };
}

export function CategoryRadarChart({ metrics, series }: CategoryRadarChartProps) {
  const [hiddenItems, setHiddenItems] = useState<RadarHiddenItem[]>([]);
  const knownSeriesIds = useMemo(() => new Set(series.map((s) => s.id)), [series]);

  const handleHiddenItemsChange = useCallback(
    (items: VisibilityIdentifier<'radar'>[]) => {
      const normalized = items
        .map((item) => toRadarHiddenItem(item, knownSeriesIds))
        .filter((item): item is RadarHiddenItem => item != null);

      if (normalized.length >= series.length) {
        // Controlled mode: re-apply current hidden state so the chart reverts the rejected toggle.
        setHiddenItems((prev) => [...prev]);
        return;
      }
      setHiddenItems(normalized);
    },
    [knownSeriesIds, series.length],
  );

  return (
    <RadarChart
      height={400}
      shape="circular"
      radar={{ max: RATING_MAX, metrics }}
      series={series.map((s) => ({
        type: 'radar' as const,
        id: s.id,
        label: s.label,
        data: s.data,
        color: s.color,
        fillArea: true,
        valueFormatter: (value: number | null, context: { dataIndex: number }) => {
          if (value == null || s.nullMask[context.dataIndex]) return 'N/A';
          return value.toFixed(1);
        },
      }))}
      hiddenItems={hiddenItems}
      onHiddenItemsChange={handleHiddenItemsChange}
      slotProps={{
        legend: {
          direction: 'horizontal',
          toggleVisibilityOnClick: true,
        },
      }}
      margin={{ top: 40, bottom: 40 }}
    />
  );
}
