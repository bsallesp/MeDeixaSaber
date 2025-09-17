import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NewsItem } from '../../../models/news-item';

@Component({
  selector: 'app-side-news-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './side-news-card.component.html',
  styleUrl: './side-news-card.component.css'
})
export class SideNewsCardComponent {
  @Input({ required: true }) item!: NewsItem;
}
