export interface NewsItem {
  id: string
  title: string
  postDate: string
  description: string
  summary?: string
  tags?: string[]
  url: string
  imageUrl?: string
}
