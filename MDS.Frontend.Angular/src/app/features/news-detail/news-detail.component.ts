import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { switchMap } from 'rxjs/operators';
import { EMPTY } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { NewsItem } from '../../models/news-item';

@Component({
  selector: 'app-news-detail',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './news-detail.component.html',
  styleUrl: './news-detail.component.css'
})
export default class NewsDetailComponent {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);

  item = signal<NewsItem | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  constructor() {
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (!id) {
          this.error.set('ID da notícia não encontrado.');
          this.loading.set(false);
          return EMPTY;
        }
        return this.api.getNewsItem(id);
      })
    ).subscribe({
      next: data => {
        this.item.set(data);
        this.loading.set(false);
      },
      error: e => {
        const msg = e?.status ? `${e.status} ${e.statusText}` : 'Erro ao carregar notícia.';
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }
}
