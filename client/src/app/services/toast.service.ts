import { Injectable, signal } from '@angular/core';

export interface ToastMsg {
  text: string;
  type: 'success' | 'error';
  id: number;
}

/** Strip technical prefixes and limit length for production-friendly error messages. */
function friendlyError(err: any, fallback = 'Something went wrong. Please try again.'): string {
  if (!err) return fallback;

  // Just a string
  if (typeof err === 'string') return truncate(err);

  // API error body fields
  const apiMsg = err?.error?.error || err?.error?.message || err?.error?.title;
  if (typeof apiMsg === 'string' && apiMsg.length > 0) return truncate(apiMsg);

  if (err?.message && typeof err.message === 'string') {
    // Strip Angular HTTP noise: "Http failure response for https://... : 500 OK"
    const cleaned = err.message.replace(/^Http failure (?:response for|during) .+?: \d+ .*$/, '');
    if (cleaned.trim()) return truncate(cleaned.trim());
  }

  return fallback;
}

function truncate(s: string, max = 200): string {
  return s.length > max ? s.slice(0, max) + '…' : s;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<ToastMsg[]>([]);
  private nextId = 0;

  show(msg: string, type: 'success' | 'error' = 'success'): void {
    const id = this.nextId++;
    this.toasts.update(t => [...t, { text: msg, type, id }]);
    setTimeout(() => this.dismiss(id), 3800);
  }

  /** Show a production-safe error toast from any caught value. */
  error(err: any, fallback?: string): void {
    this.show(friendlyError(err, fallback), 'error');
  }

  dismiss(id: number): void {
    this.toasts.update(t => t.filter(x => x.id !== id));
  }
}
