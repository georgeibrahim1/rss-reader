export function extractErrorMessage(err: any): string {
  const body = err?.error;
  if (!body) return 'Something went wrong. Please try again.';
  if (typeof body === 'string') return body;
  if (body.errors && typeof body.errors === 'object' && !Array.isArray(body.errors)) {
    const all = Object.values(body.errors).flat();
    return (all as string[]).join(' ');
  }
  return body.error || body.title || 'Something went wrong. Please try again.';
}
