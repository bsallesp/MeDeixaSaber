import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../services/api.service';
import NewsDetailComponent from './news-detail.component';
import { NewsItem } from '../../models/news-item';

describe('NewsDetailComponent', () => {
  let component: NewsDetailComponent;
  let fixture: ComponentFixture<NewsDetailComponent>;
  let mockApiService: jasmine.SpyObj<ApiService>;

  const mockNewsItem: NewsItem = {
    id: '123',
    title: 'Super Notícia de Teste',
    description: 'Descrição completa da notícia.',
    postDate: new Date().toISOString(),
    url: 'http://example.com/123'
  };

  const setup = (routeParams: { id: string | null }) => {
    mockApiService = jasmine.createSpyObj('ApiService', ['getNewsItem']);

    TestBed.configureTestingModule({
      imports: [NewsDetailComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(new Map(Object.entries({ get: (key: string) => routeParams[key as keyof typeof routeParams] })))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NewsDetailComponent);
    component = fixture.componentInstance;
  };

  it('deve exibir estado de carregando e depois mostrar a notícia', () => {
    setup({ id: '123' });
    mockApiService.getNewsItem.and.returnValue(of(mockNewsItem));

    // Estado inicial
    expect(component.loading()).toBe(true);

    // Dispara a detecção de mudanças para rodar o construtor e a subscrição
    fixture.detectChanges();

    // Após a API retornar
    expect(component.loading()).toBe(false);
    expect(component.item()).toEqual(mockNewsItem);
    expect(mockApiService.getNewsItem).toHaveBeenCalledWith('123');

    // Verifica o template HTML
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.news-article__title')?.textContent).toContain('Super Notícia de Teste');
  });

  it('deve exibir uma mensagem de erro se a API falhar', () => {
    setup({ id: '456' });
    const errorResponse = { status: 500, statusText: 'Internal Server Error' };
    mockApiService.getNewsItem.and.returnValue(throwError(() => errorResponse));

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.item()).toBeNull();
    expect(component.error()).toBe('500 Internal Server Error');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h2')?.textContent).toContain('Erro');
  });

  it('deve definir erro se o ID da rota não for encontrado', () => {
    setup({ id: null });

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.error()).toBe('ID da notícia não encontrado.');
  });
});
