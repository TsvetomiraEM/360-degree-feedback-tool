import { useCallback, useState } from 'react';
import { RadarChart } from '@mui/x-charts/RadarChart';
import type { VisibilityIdentifier } from '@mui/x-charts/plugins';
import { RATING_MAX, type RadarSeriesSummary } from './resultsChartUtils';

type CategoryRadarChartProps = {
  metrics: string[];
  series: RadarSeriesSummary[];
};

export function CategoryRadarChart({ metrics, series }: CategoryRadarChartProps) {
  const [hiddenItems, setHiddenItems] = useState<VisibilityIdentifier<'radar'>[]>([]);

  const handleHiddenItemsChange = useCallback(
    (items: VisibilityIdentifier<'radar'>[]) => {
      if (items.length >= series.length) return;
      setHiddenItems(items);
    },
    [series.length],
  );

  return (
    <RadarChart
      height={400}
      shape="circular"
      radar={{ max: RATING_MAX, metrics }}
      series={series.map((s) => ({
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
