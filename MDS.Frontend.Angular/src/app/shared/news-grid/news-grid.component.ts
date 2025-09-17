import { Component, Input } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { NewsItem } from '../../models/news-item';
import { HeadlineCardComponent } from '../news-cards/headline-card.component';

@Component({
  selector: 'app-news-grid',
  standalone: true,
  imports: [CommonModule, NgClass, HeadlineCardComponent],
  templateUrl: './news-grid.component.html',
  styleUrl: './news-grid.component.css'
})
export class NewsGridComponent {
  @Input() items: NewsItem[] = [];

  public getCardClass(index: number): string {
    return `card-position-${index}`;
  }
}
