// src/app/features/news-top/news-top.component.ts
import { Component, inject, signal } from '@angular/core'
import { CommonModule } from '@angular/common'
import { ApiService } from '../../services/api.service'
import { NewsItem } from '../../models/news-item'
import { NewsGridComponent } from '../../shared/news-grid/news-grid.component'

@Component({
  selector: 'app-news-top',
  standalone: true,
  imports: [CommonModule, NewsGridComponent],
  templateUrl: './news-top.component.html'
})
export default class NewsTopComponent {
  private api = inject(ApiService)
  items = signal<NewsItem[] | null>(null)
  loading = signal(true)
  error = signal<string | null>(null)

  constructor() {
    this.api.getNewsTop(20, 0).subscribe({
      next: x => { this.items.set(x); this.loading.set(false) },
      error: e => { this.error.set(e?.status ? `${e.status} ${e.statusText}` : 'erro'); this.loading.set(false) }
    })
  }
}
