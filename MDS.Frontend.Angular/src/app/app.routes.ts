import { Routes } from '@angular/router'

export const routes: Routes = [
  { path: '', redirectTo: 'news', pathMatch: 'full' },
  { path: 'news', loadComponent: () => import('./features/news-top/news-top.component') },
  { path: 'news/:id', loadComponent: () => import('./features/news-detail/news-detail.component') },
  { path: 'classifieds', loadComponent: () => import('./features/classifieds-top/classifieds-top.component') }
]
