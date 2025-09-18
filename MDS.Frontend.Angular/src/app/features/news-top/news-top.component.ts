import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { NewsItem } from '../../models/news-item';
import { NewsGridComponent } from '../../shared/news-grid/news-grid.component';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-news-top',
  imports: [CommonModule, NewsGridComponent, SkeletonModule],
  templateUrl: './news-top.component.html',
  styleUrl: './news-top.component.css'
})
export default class NewsTopComponent {
  private api = inject(ApiService);
  items = signal<NewsItem[] | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  constructor() {
    this.api.getNewsTop(20, 0).subscribe({
      next: x => { this.items.set(x); this.loading.set(false); },
      error: e => {
        const msg = e?.status ? `${e.status} ${e.statusText}` : 'erro';
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }
}
