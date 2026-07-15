import { Component, inject, effect } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from './components/header/header.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { ArticleCardComponent } from './components/article-card/article-card.component';
import { ToastComponent } from './components/toast/toast.component';
import { ModalComponent } from './components/modal/modal.component';
import { FeedService } from './services/feed.service';
import { ArticleService } from './services/article.service';
import { AuthService } from './services/auth.service';
import { UiService } from './services/ui.service';
import { ToastService } from './services/toast.service';
import { PlaylistService } from './services/playlist.service';
import { Feed } from './models/feed';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule, HeaderComponent, SidebarComponent, ArticleCardComponent, ToastComponent, ModalComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  readonly feedService = inject(FeedService);
  readonly articleService = inject(ArticleService);
  readonly authService = inject(AuthService);
  readonly uiService = inject(UiService);
  readonly toastService = inject(ToastService);
  readonly playlistService = inject(PlaylistService);

  modalVisible = false;
  modalMessage = '';
  private feedToDelete: Feed | null = null;

  readonly HUES = [8, 24, 40, 160, 172, 190, 204, 340];
  stationColor(id: string): string {
    let h = 0;
    for (const c of id) h = (h * 31 + c.charCodeAt(0)) >>> 0;
    return `hsl(${this.HUES[h % this.HUES.length]} 72% 52%)`;
  }

  constructor() {
    this.authService.check();

    effect(() => {
      if (this.authService.user() && !this.authService.loading()) {
        this.feedService.loadFeeds().then(() => {
          this.articleService.loadArticles(true, null);
        });
      }
    });
  }

  onDeleteRequest(f: Feed): void {
    this.feedToDelete = f;
    this.modalMessage = `Are you sure you want to remove "${f.title || f.url}"?`;
    this.modalVisible = true;
  }

  async onConfirmDelete(): Promise<void> {
    this.modalVisible = false;
    if (!this.feedToDelete) return;
    try {
      await this.feedService.removeFeed(this.feedToDelete.id);
      this.toastService.show('Feed removed');
      await this.feedService.loadFeeds();
      await this.articleService.loadArticles(true, this.feedService.getSelectedIdsParam());
    } catch (err: any) { this.toastService.show(err.message, 'error'); }
    this.feedToDelete = null;
  }

  onCancelDelete(): void {
    this.modalVisible = false;
    this.feedToDelete = null;
  }

  get totalPages(): number { return Math.ceil(this.articleService.totalCount() / this.articleService.pageSize) || 1; }

  prevPage(): void {
    if (this.articleService.page() > 1) {
      document.getElementById('main')?.scrollTo({ top: 0, behavior: 'instant' as ScrollBehavior });
      this.articleService.goToPage(this.articleService.page() - 1, this.feedService.getSelectedIdsParam());
    }
  }

  nextPage(): void {
    if (this.articleService.page() < this.totalPages) {
      document.getElementById('main')?.scrollTo({ top: 0, behavior: 'instant' as ScrollBehavior });
      this.articleService.goToPage(this.articleService.page() + 1, this.feedService.getSelectedIdsParam());
    }
  }

  get emptyStateHtml(): string {
    // 1. Search/filter active
    if (this.articleService.searchQuery() || this.articleService.dateFrom() || this.articleService.dateTo()) {
      let desc = 'No matches found';
      const q = this.articleService.searchQuery();
      if (q) desc += ` for &ldquo;<strong>${this.escapeHtml(q)}</strong>&rdquo;`;
      if (this.articleService.dateFrom() || this.articleService.dateTo()) desc += ' in the selected date range';
      desc += '.';
      return `<div class="glyph">🔍</div><h2>${desc}</h2><p>Try adjusting your search or clearing the filters.</p>`;
    }

    // 2. Playlists tab — nothing selected
    if (this.uiService.viewMode() === 'playlists' && !this.playlistService.selectedId()) {
      return '<div class="glyph">📁</div><h2>No playlist selected</h2><p>Choose a playlist from the sidebar to see its articles.</p>';
    }

    // 3. Playlists tab — selected but empty
    if (this.uiService.viewMode() === 'playlists' && this.playlistService.selectedId()) {
      return '<div class="glyph">📁</div><h2>This playlist has no articles</h2><p>Hit <strong>↻</strong> beside the playlist to pull in the latest articles.</p>';
    }

    // 4. No feed selected
    if (this.feedService.selectedIds().size === 0 && !this.feedService.allMode()) {
      return '<div class="glyph">📭</div><h2>No feed selected</h2><p>Choose feeds from the sidebar or select <strong>All Feeds</strong> to see articles.</p>';
    }

    // 5. Single feed selected, empty
    if (this.feedService.selectedIds().size === 1) {
      return '<div class="glyph">📭</div><h2>This feed has no articles</h2><p>Hit <strong>↻</strong> beside the feed to pull in the latest articles.</p>';
    }

    // 6. Starred filter active — 0 results
    if (this.articleService.starredOnly()) {
      return '<div class="glyph">⭐</div><h2>No starred articles</h2><p>Star some feeds (click ☆) to see them here.</p>';
    }

    // 7. First boot — no feeds at all
    if (this.feedService.feeds().length === 0) {
      return '<div class="glyph">📭</div><h2>Add a feed and hit <strong>↻ Refresh All</strong> to pull in the latest articles.</p>';
    }

    // 8. Default — has feeds but 0 articles
    return '<div class="glyph">📭</div><h2>No articles found</h2><p>Hit <strong>↻ Refresh All</strong> or the <strong>↻</strong> button beside a feed to pull in the latest.</p>';
  }

  get showEmpty(): boolean { return !this.articleService.loading() && this.articleService.articles().length === 0; }

  get showPagination(): boolean {
    return !this.articleService.loading() && this.articleService.articles().length > 0 && this.totalPages > 1;
  }

  get resultsSummaryText(): string | null {
    const q = this.articleService.searchQuery(), df = this.articleService.dateFrom(), dt = this.articleService.dateTo();
    const st = this.articleService.starredOnly();
    if (!q && !df && !dt && !st) return null;
    const parts: string[] = [];
    if (st) parts.push("⭐ starred feeds");
    if (q) parts.push(`"${q}"`);
    let range = '';
    if (df && dt) range = `${df} – ${dt}`;
    else if (df) range = `since ${df}`;
    else if (dt) range = `until ${dt}`;
    if (range) parts.push(range);
    return `Found ${this.articleService.totalCount()} results for ${parts.join(' · ')}`;
  }

  escapeHtml(s: string | null | undefined): string {
    const d = document.createElement('div');
    d.textContent = s ?? '';
    return d.innerHTML;
  }
}
