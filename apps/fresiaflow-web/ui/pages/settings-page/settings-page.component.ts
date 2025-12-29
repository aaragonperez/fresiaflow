import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, NavigationEnd } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { filter } from 'rxjs/operators';
import { OneDriveConfigComponent } from '../../components/onedrive-config/onedrive-config.component';
import { InvoiceSourcesConfigComponent } from '../../components/invoice-sources-config/invoice-sources-config.component';
import { AccountingAccountsConfigComponent } from '../../components/accounting-accounts-config/accounting-accounts-config.component';

/**
 * Componente de página para configuración de la aplicación.
 */
@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule, FormsModule, OneDriveConfigComponent, InvoiceSourcesConfigComponent, AccountingAccountsConfigComponent],
  templateUrl: './settings-page.component.html',
  styleUrls: ['./settings-page.component.css']
})
export class SettingsPageComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  ownCompanyNames: string[] = [];
  newCompanyName = '';
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;
  activeSection: 'companies' | 'onedrive' | 'invoice-sources' | 'accounting' = 'companies';

  ngOnInit(): void {
    // Detectar la sección activa desde la ruta
    this.updateActiveSectionFromRoute();
    
    // Escuchar cambios de ruta
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateActiveSectionFromRoute();
    });
    
    this.loadSettings();
  }

  private updateActiveSectionFromRoute(): void {
    const url = this.router.url;
    if (url.includes('/settings/onedrive')) {
      this.activeSection = 'onedrive';
    } else if (url.includes('/settings/invoice-sources')) {
      this.activeSection = 'invoice-sources';
    } else if (url.includes('/settings/accounting')) {
      this.activeSection = 'accounting';
    } else {
      this.activeSection = 'companies';
    }
  }

  async loadSettings(): Promise<void> {
    this.loading = true;
    this.error = null;
    
    try {
      const response = await firstValueFrom(
        this.http.get<{ ownCompanyNames: string[] }>('/api/settings/own-companies')
      );
      this.ownCompanyNames = response.ownCompanyNames || [];
    } catch (error: any) {
      this.error = error?.message || 'Error al cargar configuración';
    } finally {
      this.loading = false;
    }
  }

  async saveSettings(): Promise<void> {
    this.loading = true;
    this.error = null;
    this.successMessage = null;
    
    try {
      await firstValueFrom(
        this.http.post('/api/settings/own-companies', {
          ownCompanyNames: this.ownCompanyNames
        })
      );
      this.successMessage = 'Configuración guardada. Nota: Requiere reiniciar el servidor para aplicar cambios.';
    } catch (error: any) {
      this.error = error?.message || 'Error al guardar configuración';
    } finally {
      this.loading = false;
    }
  }

  addCompany(): void {
    if (this.newCompanyName.trim()) {
      const trimmed = this.newCompanyName.trim();
      if (!this.ownCompanyNames.includes(trimmed)) {
        this.ownCompanyNames.push(trimmed);
        this.newCompanyName = '';
      }
    }
  }

  removeCompany(index: number): void {
    this.ownCompanyNames.splice(index, 1);
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.addCompany();
    }
  }
}

