import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../services/api.service';
import NewsDetailComponent from './news-detail.component';
import { NewsItem } from '../../models/news-item';

describe('NewsDetailComponent', () => {
  let fixture: ComponentFixture<NewsDetailComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockNewsItem: NewsItem = {
    id: '123',
    title: 'Super Notícia de Teste',
    publishedAt: new Date().toISOString(),
    createdAt: new Date().toISOString(),
    content: 'Conteúdo da notícia.',
    summary: 'Descrição completa da notícia.',
    url: 'http://example.com/123'
  };

  async function createComponent(routeMock: any, apiMock: any) {
    await TestBed.configureTestingModule({
      imports: [NewsDetailComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        { provide: ActivatedRoute, useValue: routeMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NewsDetailComponent);
  }

  beforeEach(() => {
    apiService = jasmine.createSpyObj('ApiService', ['getNewsItem']);
  });

  it('deve exibir estado de carregando e depois mostrar a notícia', fakeAsync(async () => {
    const activatedRouteMock = { paramMap: of({ get: (key: string) => '123' }) };
    apiService.getNewsItem.and.returnValue(of(mockNewsItem));
    await createComponent(activatedRouteMock, apiService);

    const component = fixture.componentInstance;

    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.item()).toEqual(mockNewsItem);
    expect(apiService.getNewsItem).toHaveBeenCalledWith('123');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.news-article__title')?.textContent).toContain('Super Notícia de Teste');
  }));

  it('deve exibir uma mensagem de erro se a API falhar', fakeAsync(async () => {
    const activatedRouteMock = { paramMap: of({ get: (key: string) => '123' }) };
    const errorResponse = { status: 500, statusText: 'Internal Server Error' };
    apiService.getNewsItem.and.returnValue(throwError(() => errorResponse));
    await createComponent(activatedRouteMock, apiService);

    const component = fixture.componentInstance;

    fixture.detectChanges();
    tick();

    expect(component.error()).toBe('500 Internal Server Error');
  }));

  it('deve definir erro se o ID da rota não for encontrado', fakeAsync(async () => {
    const activatedRouteMock = { paramMap: of({ get: (key: string) => null }) };
    await createComponent(activatedRouteMock, apiService);

    const component = fixture.componentInstance;

    fixture.detectChanges();
    tick();

    expect(component.error()).toBe('ID da notícia não encontrado.');
  }));
});
