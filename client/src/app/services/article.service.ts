import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Article } from '../models/article';
import { FeedService } from './feed.service';

const CACHE_TTL = 10 * 60 * 1000; // 10 minutes

interface CacheEntry {
  articles: Article[];
  totalCount: number;
  timestamp: number;
}

@Injectable({ providedIn: 'root' })
export class ArticleService {
  private http = inject(HttpClient);
  private feedService = inject(FeedService);

  readonly articles = signal<Article[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);
  readonly refreshing = signal(false); // true when fetching but articles already have data (SWR)
  readonly searchQuery = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly starredOnly = signal(false);
  readonly pageSize = 20;

  private cache = new Map<string, CacheEntry>();
  private inFlight = new Map<string, Promise<void>>(); // deduplicate concurrent requests

  private cacheKey(feedIdsParam: string | null): string {
    return [
      feedIdsParam ?? '__all__',
      this.page(),
      this.searchQuery(),
      this.dateFrom(),
      this.dateTo(),
      this.starredOnly()
    ].join('|');
  }

  /** Invalidate cache for a specific feed or all entries */
  invalidateCache(feedId?: string): void {
    if (!feedId) { this.cache.clear(); return; }
    for (const key of this.cache.keys()) {
      if (key.includes(feedId)) this.cache.delete(key);
    }
  }

  async loadArticles(replace = false, feedIdsParam: string | null = null): Promise<void> {
    // Only guard when no explicit feedIds — playlist/user-specified IDs bypass selection state
    if (!feedIdsParam && !this.feedService.allMode() && this.feedService.selectedIds().size === 0) {
      this.articles.set([]);
      this.totalCount.set(0);
      this.loading.set(false);
      this.refreshing.set(false);
      return;
    }

    if (replace) this.page.set(1);

    const key = this.cacheKey(feedIdsParam);

    // Dedup: if same request is already in-flight, re-use it
    const existing = this.inFlight.get(key);
    if (existing) return existing;

    // SWR: only show full skeleton on first empty load — otherwise keep old data
    const hasData = this.articles().length > 0;
    if (!hasData) {
      this.loading.set(true);
    } else {
      this.refreshing.set(true);
    }

    const cached = this.cache.get(key);

    // Use cache if fresh (skip on explicit replace)
    if (!replace && cached && (Date.now() - cached.timestamp) < CACHE_TTL) {
      this.articles.set(cached.articles);
      this.totalCount.set(cached.totalCount);
      this.loading.set(false);
      this.refreshing.set(false);
      return;
    }

    const promise = (async () => {
      try {
        const params = new URLSearchParams({ page: String(this.page()), pageSize: String(this.pageSize) });
        if (feedIdsParam) params.set('feedIds', feedIdsParam);
        if (this.searchQuery()) params.set('q', this.searchQuery());
        if (this.dateFrom()) params.set('dateFrom', this.dateFrom());
        if (this.dateTo()) params.set('dateTo', this.dateTo());
        if (this.starredOnly()) params.set('starred', 'true');

        const data = await firstValueFrom(
          this.http.get<{ articles: Article[]; totalCount: number }>(`/articles?${params}`)
        );
        this.cache.set(key, { articles: data.articles, totalCount: data.totalCount, timestamp: Date.now() });
        this.articles.set(data.articles);
        this.totalCount.set(data.totalCount);
      } catch {
        // On error with existing data: keep old data visible, don't wipe
        if (!hasData) {
          this.articles.set([]);
          this.totalCount.set(0);
        }
      } finally {
        this.loading.set(false);
        this.refreshing.set(false);
        this.inFlight.delete(key);
      }
    })();

    this.inFlight.set(key, promise);
    return promise;
  }

  goToPage(n: number, feedIdsParam: string | null = null): void {
    this.page.set(n);
    this.loadArticles(false, feedIdsParam);
  }
}
