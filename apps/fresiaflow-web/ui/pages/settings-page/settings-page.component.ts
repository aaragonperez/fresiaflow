import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

/**
 * Componente de página para configuración de la aplicación.
 */
@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings-page.component.html',
  styleUrls: ['./settings-page.component.css']
})
export class SettingsPageComponent implements OnInit {
  private http = inject(HttpClient);
  
  ownCompanyNames: string[] = [];
  newCompanyName = '';
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.loadSettings();
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

