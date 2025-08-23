import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Menu } from './menu/menu';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Menu],
  templateUrl: 'app.html',
  styleUrls: ['app.css']
})
export class App {
  private t = inject(TranslateService);
  constructor() {
    this.t.setDefaultLang('pt');
    this.t.use('pt');
  }
}
