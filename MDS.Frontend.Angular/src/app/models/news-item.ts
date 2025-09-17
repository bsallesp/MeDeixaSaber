export interface NewsItem {
  id: string;
  title: string;
  publishedAt: string;
  createdAt: string;
  content: string;
  summary?: string;
  tags?: string[];
  url: string;
  imageUrl?: string;
}
