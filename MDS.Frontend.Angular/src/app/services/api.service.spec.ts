import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiService } from './api.service';
import { NewsItem } from '../models/news-item';
import { ClassifiedItem } from '../models/classified-item';

describe('ApiService', () => {
  let service: ApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApiService
      ]
    });
    service = TestBed.inject(ApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getNewsTop should make a GET request to the correct URL', () => {
    const mockNews: NewsItem[] = [{ id: '1', title: 'News 1', postDate: '2025-01-01', description: 'Desc', url: '' }];
    const take = 15;

    service.getNewsTop(take).subscribe(data => {
      expect(data).toEqual(mockNews);
    });

    const req = httpTestingController.expectOne(`/api/news/top?pageSize=${take}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockNews);
  });

  it('getNewsItem should make a GET request to the correct URL with id', () => {
    const mockNewsItem: NewsItem = { id: '42', title: 'News 42', postDate: '2025-01-01', description: 'Desc', url: '' };
    const newsId = '42';

    service.getNewsItem(newsId).subscribe(data => {
      expect(data).toEqual(mockNewsItem);
    });

    const req = httpTestingController.expectOne(`/api/news/${newsId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockNewsItem);
  });

  it('getClassifiedsTop should make a GET request to the correct URL with take and skip', () => {
    const mockClassifieds: ClassifiedItem[] = [{ id: 'c1', title: 'Classified 1', postDate: '2025-01-01', description: 'Desc', url: '' }];
    const take = 10;
    const skip = 5;

    service.getClassifiedsTop(take, skip).subscribe(data => {
      expect(data).toEqual(mockClassifieds);
    });

    const req = httpTestingController.expectOne(`/api/classifieds/top?take=${take}&skip=${skip}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockClassifieds);
  });
});
