import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface Playlist {
  id: string;
  name: string;
  feedCount: number;
  emoji?: string;
}

@Injectable({ providedIn: 'root' })
export class PlaylistService {
  private http = inject(HttpClient);

  readonly playlists = signal<Playlist[]>([]);
  readonly selectedId = signal<string | null>(null);

  async load(): Promise<void> {
    const data = await firstValueFrom(this.http.get<Playlist[]>('/playlists'));
    this.playlists.set(data);
  }

  async create(name: string): Promise<void> {
    await firstValueFrom(this.http.post(`/playlists?name=${encodeURIComponent(name)}`, null));
    await this.load();
  }

  async rename(id: string, name: string, emoji?: string): Promise<void> {
    const params = new URLSearchParams({ name });
    if (emoji) params.set('emoji', emoji);
    await firstValueFrom(this.http.put(`/playlists/${id}?${params}`, null));
    await this.load();
  }

  async remove(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/playlists/${id}`));
    if (this.selectedId() === id) this.selectedId.set(null);
    await this.load();
  }

  async refresh(id: string): Promise<{ articleCount: number; failed?: { id: string; title: string; error: string }[] }> {
    return firstValueFrom(this.http.post<{ articleCount: number; failed?: { id: string; title: string; error: string }[] }>(`/playlists/${id}/refresh`, null));
  }

  async addFeed(playlistId: string, feedId: string): Promise<void> {
    await firstValueFrom(this.http.post(`/playlists/${playlistId}/feeds?feedId=${feedId}`, null));
    await this.load();
  }

  async removeFeed(playlistId: string, feedId: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/playlists/${playlistId}/feeds/${feedId}`));
    await this.load();
  }

  select(id: string | null): void { this.selectedId.set(id); }

  async starPlaylist(id: string): Promise<{ starCount: number; starred: boolean }> {
    return firstValueFrom(this.http.post<{ starCount: number; starred: boolean }>(`/playlists/${id}/star`, null));
  }

  async toggleEmailNotifications(id: string): Promise<{ emailCount: number; enabled: boolean }> {
    return firstValueFrom(this.http.post<{ emailCount: number; enabled: boolean }>(`/playlists/${id}/email-notifications`, null));
  }
}
