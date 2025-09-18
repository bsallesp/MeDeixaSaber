import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HeadlineCardComponent } from './headline-card.component';
import { NewsItem } from '../../../models/news-item';

describe('HeadlineCardComponent', () => {
  let component: HeadlineCardComponent;
  let fixture: ComponentFixture<HeadlineCardComponent>;

  const mockItem: NewsItem = {
    id: 'test-1',
    title: 'Super Notícia de Teste',
    summary: 'Este é um resumo da notícia.',
    publishedAt: new Date().toISOString(),
    createdAt: new Date().toISOString(),
    content: 'Conteúdo completo.',
    url: 'http://example.com/test-1',
    imageUrl: 'http://example.com/image.jpg'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeadlineCardComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(HeadlineCardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the news item properties in the template', () => {
    component.item = mockItem;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const titleElement = compiled.querySelector('.card__title');
    const imageElement = compiled.querySelector('.card__image') as HTMLImageElement;
    const linkElement = compiled.querySelector('.card__link') as HTMLAnchorElement;

    expect(titleElement?.textContent).toContain(mockItem.title);
    expect(imageElement).toBeTruthy();
    expect(imageElement.src).toContain(mockItem.imageUrl);
    expect(imageElement.alt).toBe(mockItem.title);
    expect(linkElement.getAttribute('href')).toBe(`/news/${mockItem.id}`);
  });

  it('should not display an image element if imageUrl is not provided', () => {
    const itemWithoutImage = { ...mockItem, imageUrl: undefined };
    component.item = itemWithoutImage;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const imageElement = compiled.querySelector('.card__image');

    expect(imageElement).toBeNull();
  });
});
