import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import NewsTopComponent from './news-top.component';
import { ApiService } from '../../services/api.service';
import { NewsItem } from '../../models/news-item';
import { NewsGridComponent } from '../../shared/news-grid/news-grid.component';

describe('NewsTopComponent', () => {
  let component: NewsTopComponent;
  let fixture: ComponentFixture<NewsTopComponent>;
  let apiService: ApiService;

  const mockNewsItems: NewsItem[] = [
    { id: '1', title: 'Test News 1', postDate: new Date().toISOString(), description: 'Desc 1', url: 'http://test.com/1' },
    { id: '2', title: 'Test News 2', postDate: new Date().toISOString(), description: 'Desc 2', url: 'http://test.com/2' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewsTopComponent, NewsGridComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApiService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NewsTopComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ApiService);
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should show loading state initially', () => {
    expect(component.loading()).toBe(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('carregando...');
  });

  it('should load news items on init and hide loading', () => {
    spyOn(apiService, 'getNewsTop').and.returnValue(of(mockNewsItems));
    fixture.detectChanges();
    expect(component.loading()).toBe(false);
    expect(component.items()).toEqual(mockNewsItems);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-news-grid')).not.toBeNull();
    expect(compiled.textContent).not.toContain('carregando...');
  });

  it('should show error message on api error', () => {
    const errorResponse = { status: 500, statusText: 'Server Error' };
    spyOn(apiService, 'getNewsTop').and.returnValue(throwError(() => errorResponse));
    fixture.detectChanges();
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe('500 Server Error');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('500 Server Error');
    expect(compiled.querySelector('app-news-grid')).toBeNull();
  });
});
