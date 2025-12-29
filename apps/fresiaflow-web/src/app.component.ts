import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout.component';
import { ThemeService } from '../infrastructure/services/theme.service';
import { ChristmasDecorationsComponent } from '../ui/components/christmas-decorations/christmas-decorations.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, MainLayoutComponent, ChristmasDecorationsComponent],
  template: `
    <app-christmas-decorations></app-christmas-decorations>
    <app-main-layout></app-main-layout>
  `
})
export class AppComponent implements OnInit {
  title = 'FresiaFlow';
  private themeService = inject(ThemeService);

  ngOnInit(): void {
    // Inicializar el servicio de temas para aplicar el tema guardado
    this.themeService.currentTheme();
  }
}

