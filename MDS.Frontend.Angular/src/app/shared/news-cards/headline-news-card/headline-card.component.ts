import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NewsItem } from '../../../models/news-item';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-headline-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './headline-card.component.html',
  styleUrl: './headline-card.component.css'
})
export class HeadlineCardComponent {
  @Input({ required: true }) item!: NewsItem;
}
