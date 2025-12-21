import { Component, inject } from '@angular/core';
// TODO: Importar InvoiceFacade desde web o crear uno específico para mobile
// import { InvoiceFacade } from '../../application/invoice.facade';

/**
 * Página para subir documentos (facturas) desde móvil.
 */
@Component({
  selector: 'app-upload-document-page',
  standalone: true,
  templateUrl: './upload-document-page.component.html',
  styleUrls: ['./upload-document-page.component.css']
})
export class UploadDocumentPageComponent {
  // TODO: Inyectar InvoiceFacade cuando esté disponible
  // invoiceFacade = inject(InvoiceFacade);

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      // TODO: Implementar upload
      // await this.invoiceFacade.uploadInvoice(file);
      console.log('Upload file:', file.name);
    }
  }
}

