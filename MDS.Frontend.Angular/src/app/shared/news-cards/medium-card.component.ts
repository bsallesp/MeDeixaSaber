import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface MediumItem {
  title: string;
  description?: string;
  postDate?: string;
  url: string;
  tags?: string[];
}

@Component({
  selector: 'app-medium-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medium-card.component.html',
  styleUrl: './medium-card.component.css'
})
export class MediumCardComponent {
  @Input() item!: MediumItem;
}
