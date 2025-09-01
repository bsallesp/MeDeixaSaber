// src/app/features/classifieds-top/classifieds-top.component.ts
import { Component, inject, signal } from '@angular/core'
import { CommonModule } from '@angular/common'
import { ApiService } from '../../services/api.service'
import { ClassifiedItem } from '../../models/classified-item'

@Component({
    selector: 'app-classifieds-top',
    imports: [CommonModule],
    templateUrl: './classifieds-top.component.html'
})
export default class ClassifiedsTopComponent {
  private api = inject(ApiService)
  items = signal<ClassifiedItem[] | null>(null)
  loading = signal<boolean>(true)
  error = signal<string | null>(null)

  constructor() {
    this.api.getClassifiedsTop(20, 0).subscribe({
      next: x => { this.items.set(x); this.loading.set(false) },
      error: e => {
        const msg = e?.status ? `${e.status} ${e.statusText}` : 'erro'
        this.error.set(msg)
        this.loading.set(false)
      }
    })
  }
}
