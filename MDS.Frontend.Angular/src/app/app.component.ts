import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TopNavComponent } from './shared/top-nav/top-nav.component';
import { TopSide } from './features/top-side/top-side';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, TopNavComponent, TopSide],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {}
