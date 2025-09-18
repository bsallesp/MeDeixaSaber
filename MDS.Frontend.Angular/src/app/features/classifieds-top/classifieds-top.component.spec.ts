import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../services/api.service';
import ClassifiedsTopComponent from './classifieds-top.component';
import { ClassifiedItem } from '../../models/classified-item';

describe('ClassifiedsTopComponent', () => {
  let fixture: ComponentFixture<ClassifiedsTopComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockClassifieds: ClassifiedItem[] = [
    { id: '1', title: 'Test Classified 1', postDate: new Date().toISOString(), description: 'Desc 1', url: 'http://test.com/1' },
    { id: '2', title: 'Test Classified 2', postDate: new Date().toISOString(), description: 'Desc 2', url: 'http://test.com/2' }
  ];

  async function createComponent(apiMock: any) {
    await TestBed.configureTestingModule({
      imports: [ClassifiedsTopComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClassifiedsTopComponent);
  }

  beforeEach(() => {
    apiService = jasmine.createSpyObj('ApiService', ['getClassifiedsTop']);
  });

  it('should create', fakeAsync(async () => {
    apiService.getClassifiedsTop.and.returnValue(of([]));
    await createComponent(apiService);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  }));

  it('should load classifieds on init', fakeAsync(async () => {
    apiService.getClassifiedsTop.and.returnValue(of(mockClassifieds));
    await createComponent(apiService);
    const component = fixture.componentInstance;

    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.items()).toEqual(mockClassifieds);
    expect(component.error()).toBeNull();
  }));

  it('should show an error message if the api fails', fakeAsync(async () => {
    const errorResponse = { status: 500, statusText: 'Server Error' };
    apiService.getClassifiedsTop.and.returnValue(throwError(() => errorResponse));
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
