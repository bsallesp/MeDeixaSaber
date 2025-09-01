import { inject, Injectable } from '@angular/core'
import { HttpClient } from '@angular/common/http'
import { Observable } from 'rxjs'
import { NewsItem } from '../models/news-item'
import { ClassifiedItem } from '../models/classified-item'

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient)

  getNewsTop(take = 20, skip = 0): Observable<NewsItem[]> {
    return this.http.get<NewsItem[]>(`/api/news/top?pageSize=${take}`)
  }

  getClassifiedsTop(take = 20, skip = 0): Observable<ClassifiedItem[]> {
    return this.http.get<ClassifiedItem[]>(`/api/classifieds/top?take=${take}&skip=${skip}`)
  }
}
