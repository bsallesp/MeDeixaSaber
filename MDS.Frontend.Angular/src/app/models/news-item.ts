// src/app/models/news-item.ts
export interface NewsItem {
  id: string
  title: string
  postDate: string
  description: string
  tags?: string[]
  url: string
}
