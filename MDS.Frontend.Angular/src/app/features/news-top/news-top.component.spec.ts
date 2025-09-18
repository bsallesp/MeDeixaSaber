import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../services/api.service';
import NewsTopComponent from './news-top.component';
import { NewsItem } from '../../models/news-item';

describe('NewsTopComponent', () => {
  let fixture: ComponentFixture<NewsTopComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockNewsItems: NewsItem[] = [
    { id: '1', title: 'Test News 1', publishedAt: new Date().toISOString(), createdAt: '', content: '', summary: 'Desc 1', url: 'http://test.com/1' },
    { id: '2', title: 'Test News 2', publishedAt: new Date().toISOString(), createdAt: '', content: '', summary: 'Desc 2', url: 'http://test.com/2' }
  ];

  async function createComponent(apiMock: any) {
    await TestBed.configureTestingModule({
      imports: [NewsTopComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NewsTopComponent);
  }

  beforeEach(() => {
    apiService = jasmine.createSpyObj('ApiService', ['getNewsTop']);
  });

  it('should load news items on init and hide loading', fakeAsync(async () => {
    apiService.getNewsTop.and.returnValue(of(mockNewsItems));
    await createComponent(apiService);

    const component = fixture.componentInstance;
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.items()).toEqual(mockNewsItems);
    expect(component.error()).toBeNull();
  }));

  it('should show error message on api error', fakeAsync(async () => {
    const errorResponse = { status: 500, statusText: 'Server Error' };
    apiService.getNewsTop.and.returnValue(throwError(() => errorResponse));
    await createComponent(apiService);

    const component = fixture.componentInstance;
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.error()).toBe('500 Server Error');
    expect(component.items()).toBeNull();
  }));
});
