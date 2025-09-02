import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SmallItem {
  title: string;
  url: string;
}

@Component({
  selector: 'app-small-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './small-card.component.html',
  styleUrl: './small-card.component.css'
})
export class SmallCardComponent {
  @Input() item!: SmallItem;
}
