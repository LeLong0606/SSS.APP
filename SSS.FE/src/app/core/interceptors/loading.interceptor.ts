import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpResponse
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, finalize } from 'rxjs/operators';

import { LoadingService } from '../services/loading.service';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  private totalRequests = 0;

  constructor(private loadingService: LoadingService) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip loading for certain requests (like image uploads with progress tracking)
    const skipLoading = request.headers.has('Skip-Loading') || 
                       request.url.includes('/image/view/') ||
                       request.url.includes('/export') ||
                       request.responseType === 'blob';

    if (!skipLoading) {
      this.totalRequests++;
      this.loadingService.setLoading(true);
    }

    return next.handle(request).pipe(
      tap(event => {
        if (event instanceof HttpResponse && !skipLoading) {
          // Request completed successfully
        }
      }),
      finalize(() => {
        if (!skipLoading) {
          this.totalRequests--;
          if (this.totalRequests === 0) {
            this.loadingService.setLoading(false);
          }
        }
      })
    );
  }
}
