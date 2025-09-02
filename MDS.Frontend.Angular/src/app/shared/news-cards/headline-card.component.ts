import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface HeadlineItem {
  title: string;
  description?: string;
  postDate?: string;
  url: string;
  tags?: string[];
}

@Component({
  selector: 'app-headline-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './headline-card.component.html',
  styleUrl: './headline-card.component.css'
})
export class HeadlineCardComponent {
  @Input() item!: HeadlineItem;
}
