export function suffix() {
  return Date.now().toString();
}

export function uniqueEmail(prefix: string) {
  return `${prefix}-${suffix()}@feedback360.local`;
}
