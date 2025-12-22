import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { ThemeService, Theme } from '../../../infrastructure/services/theme.service';

@Component({
  selector: 'app-theme-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule],
  template: `
    <div class="theme-selector">
      <label for="theme-select" class="theme-label">
        <i class="pi pi-palette"></i>
        Tema
      </label>
      <p-dropdown
        id="theme-select"
        [options]="themeOptions"
        [(ngModel)]="selectedTheme"
        (onChange)="onThemeChange()"
        optionLabel="label"
        optionValue="value"
        [style]="{ width: '100%' }"
        [showClear]="false">
      </p-dropdown>
    </div>
  `,
  styles: [`
    .theme-selector {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      padding: 1rem;
    }

    .theme-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-color, #1f2937);
    }

    .theme-label i {
      color: var(--primary-color, #dc2626);
    }
  `]
})
export class ThemeSelectorComponent {
  private themeService = inject(ThemeService);
  
  selectedTheme: Theme = this.themeService.currentTheme();
  
  themeOptions = [
    { label: 'Defecto', value: 'light' as Theme },
    { label: 'Azul', value: 'blue' as Theme },
    { label: 'Verde', value: 'green' as Theme },
    { label: 'Morado', value: 'purple' as Theme },
    { label: 'Naranja', value: 'orange' as Theme },
    { label: 'Amarillo', value: 'yellow' as Theme }
  ];

  onThemeChange(): void {
    this.themeService.setTheme(this.selectedTheme);
  }
}

