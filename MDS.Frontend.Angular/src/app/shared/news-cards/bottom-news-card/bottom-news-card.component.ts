import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NewsItem } from '../../../models/news-item';

@Component({
  selector: 'bottom-news-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './bottom-news-card.component.html',
  styleUrl: './bottom-news-card.component.css'
})
export class BottomNewsCardComponent {
  @Input({ required: true }) item!: NewsItem;
}
