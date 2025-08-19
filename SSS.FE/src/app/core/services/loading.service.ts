import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  private requestCount = 0;

  constructor() {}

  setLoading(loading: boolean): void {
    if (loading) {
      this.requestCount++;
    } else {
      this.requestCount = Math.max(0, this.requestCount - 1);
    }
    
    this.loadingSubject.next(this.requestCount > 0);
  }

  isLoading(): boolean {
    return this.loadingSubject.value;
  }

  reset(): void {
    this.requestCount = 0;
    this.loadingSubject.next(false);
  }
}
