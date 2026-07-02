import { useMemo } from 'react';
import { BarChart } from '@mui/x-charts/BarChart';
import {
  RATING_MAX,
  estimateBarChartHeight,
  estimateYAxisWidth,
  maxWrappedLines,
} from './resultsChartUtils';

export type QuestionBarChartSeries = {
  dataKey: string;
  label: string;
  color: string;
};

type QuestionBarChartDatasetRow = {
  question: string;
  [key: string]: string | number;
};

type QuestionBarChartProps = {
  dataset: QuestionBarChartDatasetRow[];
  series: QuestionBarChartSeries[];
  height?: number;
  yAxisWidth?: number;
};

export function QuestionBarChart({ dataset, series, height, yAxisWidth }: QuestionBarChartProps) {
  const labels = useMemo(() => dataset.map((row) => row.question), [dataset]);
  const resolvedYAxisWidth = yAxisWidth ?? estimateYAxisWidth(labels);
  const resolvedHeight = height ?? estimateBarChartHeight(dataset.length, maxWrappedLines(labels));

  return (
    <BarChart
      layout="horizontal"
      dataset={dataset}
      yAxis={[{
        scaleType: 'band',
        dataKey: 'question',
        width: resolvedYAxisWidth,
        categoryGapRatio: 0.5,
        barGapRatio: 0.6,
        tickLabelStyle: { fontSize: 12, textAnchor: 'end' },
      }]}
      xAxis={[{
        min: 0,
        max: RATING_MAX,
        tickNumber: RATING_MAX + 1,
        domainLimit: 'strict',
      }]}
      series={series}
      height={resolvedHeight}
      margin={{ left: 8, right: 24, top: 40, bottom: 32 }}
    />
  );
}
