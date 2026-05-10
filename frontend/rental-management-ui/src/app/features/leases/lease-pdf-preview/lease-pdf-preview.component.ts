import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-lease-pdf-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './lease-pdf-preview.component.html'
})
export class LeasePdfPreviewComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private sanitizer = inject(DomSanitizer);
  private route = inject(ActivatedRoute);

  loading = true;
  error: string | null = null;
  safeUrl: SafeResourceUrl | null = null;
  private objectUrl: string | null = null;

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLeasePdf(this.id).subscribe({
      next: (blob) => {
        this.objectUrl = URL.createObjectURL(blob);
        this.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
        this.loading = false;
      },
      error: (err) => {
        this.error = err.status === 404
          ? 'Document not found.'
          : err.status === 403
          ? "You don't have access to this document."
          : 'Failed to generate document. Please try again.';
        this.loading = false;
      }
    });
  }

  ngOnDestroy() {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  download() {
    if (!this.objectUrl) return;
    const a = document.createElement('a');
    a.href = this.objectUrl;
    a.download = 'lease-agreement.pdf';
    a.click();
  }
}
