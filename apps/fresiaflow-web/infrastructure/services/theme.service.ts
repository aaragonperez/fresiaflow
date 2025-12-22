import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'blue' | 'green' | 'purple' | 'orange' | 'yellow' | 'dark';

export interface ThemeConfig {
  name: string;
  primaryColor: string;
  secondaryColor: string;
  backgroundColor: string;
  textColor: string;
  sidebarBg: string;
  cardBg: string;
}

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_STORAGE_KEY = 'fresiaflow-theme';
  
  private readonly themes: Record<Theme, ThemeConfig> = {
    light: {
      name: 'Defecto',
      primaryColor: '#dc2626',
      secondaryColor: '#fee2e2',
      backgroundColor: '#fafafa',
      textColor: '#1f2937',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    blue: {
      name: 'Azul',
      primaryColor: '#2563eb',
      secondaryColor: '#dbeafe',
      backgroundColor: '#f0f9ff',
      textColor: '#1e3a8a',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    green: {
      name: 'Verde',
      primaryColor: '#16a34a',
      secondaryColor: '#dcfce7',
      backgroundColor: '#f0fdf4',
      textColor: '#166534',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    purple: {
      name: 'Morado',
      primaryColor: '#9333ea',
      secondaryColor: '#f3e8ff',
      backgroundColor: '#faf5ff',
      textColor: '#6b21a8',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    orange: {
      name: 'Naranja',
      primaryColor: '#ea580c',
      secondaryColor: '#ffedd5',
      backgroundColor: '#fff7ed',
      textColor: '#9a3412',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    yellow: {
      name: 'Amarillo',
      primaryColor: '#ca8a04',
      secondaryColor: '#fef9c3',
      backgroundColor: '#fefce8',
      textColor: '#854d0e',
      sidebarBg: '#ffffff',
      cardBg: '#ffffff'
    },
    dark: {
      name: 'Oscuro',
      primaryColor: '#6b7280',
      secondaryColor: '#374151',
      backgroundColor: '#111827',
      textColor: '#f9fafb',
      sidebarBg: '#1f2937',
      cardBg: '#1f2937'
    }
  };

  currentTheme = signal<Theme>(this.getInitialTheme());

  constructor() {
    // Aplicar tema al inicializar
    this.applyTheme(this.currentTheme());
    
    // Aplicar tema cuando cambie
    effect(() => {
      this.applyTheme(this.currentTheme());
    });
  }

  private getInitialTheme(): Theme {
    if (typeof window !== 'undefined' && window.localStorage) {
      const saved = localStorage.getItem(this.THEME_STORAGE_KEY) as Theme;
      if (saved && this.themes[saved]) {
        return saved;
      }
    }
    return 'light';
  }

  setTheme(theme: Theme): void {
    this.currentTheme.set(theme);
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.setItem(this.THEME_STORAGE_KEY, theme);
    }
  }

  getThemeConfig(theme: Theme): ThemeConfig {
    return this.themes[theme];
  }

  getAllThemes(): Theme[] {
    return Object.keys(this.themes) as Theme[];
  }

  private applyTheme(theme: Theme): void {
    const config = this.themes[theme];
    const root = document.documentElement;
    
    root.style.setProperty('--primary-color', config.primaryColor);
    root.style.setProperty('--primary-color-strong', this.darkenColor(config.primaryColor, 15));
    root.style.setProperty('--secondary-color', config.secondaryColor);
    root.style.setProperty('--background-color', config.backgroundColor);
    root.style.setProperty('--text-color', config.textColor);
    root.style.setProperty('--sidebar-bg', config.sidebarBg);
    root.style.setProperty('--card-bg', config.cardBg);
    
    // Aplicar clase al body para estilos específicos del tema
    document.body.className = document.body.className.replace(/theme-\w+/g, '');
    document.body.classList.add(`theme-${theme}`);
  }

  private darkenColor(hex: string, percent: number): string {
    const num = parseInt(hex.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = Math.max((num >> 16) - amt, 0);
    const G = Math.max((num >> 8 & 0x00FF) - amt, 0);
    const B = Math.max((num & 0x0000FF) - amt, 0);
    return '#' + (0x1000000 + R * 0x10000 + G * 0x100 + B).toString(16).slice(1);
  }
}

