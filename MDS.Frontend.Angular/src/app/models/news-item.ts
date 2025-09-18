export interface NewsItem {
  id: string;
  title: string;
  publishedAt: string; // ✅ Notícia tem 'publishedAt'
  createdAt: string;   // ✅ E 'createdAt'
  content: string;
  summary?: string;     // ✅ E 'summary' opcional
  tags?: string[];
  url: string;
  imageUrl?: string;
  // ❌ NÂO TEM 'postDate'
  // ❌ NÂO TEM 'description'
}
