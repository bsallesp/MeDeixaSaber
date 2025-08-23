import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: 'menu.html',
  styleUrls: ['menu.css']
})
export class Menu {
  private t = inject(TranslateService);
  lang = 'pt';
  switch(l: string) { this.lang = l; this.t.use(l); }
}
