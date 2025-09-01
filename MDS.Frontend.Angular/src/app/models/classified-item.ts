// src/app/models/classified-item.ts
export interface ClassifiedItem {
  id: string
  title: string
  postDate: string
  description: string
  tags?: string[]
  url: string
}
