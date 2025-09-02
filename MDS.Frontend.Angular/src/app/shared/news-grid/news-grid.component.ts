import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeadlineCardComponent } from '../news-cards/headline-card.component';
import { MediumCardComponent } from '../news-cards/medium-card.component';
import { SmallCardComponent } from '../news-cards/small-card.component';
import { NewsItem } from '../../models/news-item';

@Component({
  selector: 'app-news-grid',
  standalone: true,
  imports: [CommonModule, HeadlineCardComponent, MediumCardComponent, SmallCardComponent],
  templateUrl: './news-grid.component.html',
  styleUrl: './news-grid.component.css'
})
export class NewsGridComponent {
  @Input() set items(value: NewsItem[] | null) {
    this._items.set(value ?? []);
  }
  private _items = signal<NewsItem[]>([]);

  headline = computed(() => this._items()[0]);
  medium = computed(() => this._items().slice(1, 5));
  small = computed(() => this._items().slice(5));
}
