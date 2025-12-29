import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChristmasThemeService } from '../../../infrastructure/services/christmas-theme.service';

interface Snowflake {
  id: number;
  left: number;
  animationDuration: number;
  animationDelay: number;
  size: number;
  opacity: number;
  symbol: string;
}

/**
 * Componente de decoraciones navideñas.
 * Incluye copos de nieve animados y elementos festivos.
 */
@Component({
  selector: 'app-christmas-decorations',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (themeService.isActive()) {
      <!-- Copos de nieve cayendo -->
      <div class="snowflakes" aria-hidden="true">
        @for (flake of snowflakes; track flake.id) {
          <div 
            class="snowflake"
            [style.left.%]="flake.left"
            [style.animation-duration.s]="flake.animationDuration"
            [style.animation-delay.s]="flake.animationDelay"
            [style.font-size.px]="flake.size"
            [style.opacity]="flake.opacity">
            {{ flake.symbol }}
          </div>
        }
      </div>

      <!-- Guirnalda superior -->
      <div class="christmas-garland">
        <div class="garland-lights">
          @for (i of lights; track i) {
            <span class="light" [style.animation-delay.s]="i * 0.15"></span>
          }
        </div>
      </div>

      <!-- Banner festivo -->
      <div class="christmas-banner" [class.hidden]="bannerDismissed">
        <div class="banner-content">
          <span class="banner-emoji">🎄</span>
          <span class="banner-text">¡Felices Fiestas! El equipo de FresiaFlow te desea una Feliz Navidad y próspero Año Nuevo</span>
          <span class="banner-emoji">🎅</span>
        </div>
        <button class="banner-close" (click)="dismissBanner()" title="Cerrar">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Botón para reactivar si se descartó -->
      @if (themeService.dismissed()) {
        <button 
          class="reactivate-btn" 
          (click)="themeService.reactivate()"
          title="Reactivar decoraciones navideñas">
          🎄
        </button>
      }
    }
  `,
  styles: [`
    /* Contenedor de copos de nieve */
    .snowflakes {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 9999;
      overflow: hidden;
    }

    .snowflake {
      position: absolute;
      top: -50px;
      color: #fff;
      text-shadow: 0 0 5px rgba(255, 255, 255, 0.8);
      animation: snowfall linear infinite;
      user-select: none;
    }

    @keyframes snowfall {
      0% {
        transform: translateY(-50px) rotate(0deg);
      }
      100% {
        transform: translateY(calc(100vh + 50px)) rotate(360deg);
      }
    }

    /* Guirnalda de luces */
    .christmas-garland {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 20px;
      z-index: 10000;
      pointer-events: none;
      background: linear-gradient(to bottom, #1a472a 0%, transparent 100%);
    }

    .garland-lights {
      display: flex;
      justify-content: space-around;
      align-items: flex-start;
      padding: 0 10px;
      height: 100%;
    }

    .light {
      width: 10px;
      height: 14px;
      border-radius: 50% 50% 50% 50% / 60% 60% 40% 40%;
      animation: twinkle 1s ease-in-out infinite alternate;
      box-shadow: 0 0 10px currentColor, 0 0 20px currentColor;
    }

    .light:nth-child(5n+1) { background: #ff6b6b; color: #ff6b6b; }
    .light:nth-child(5n+2) { background: #ffd93d; color: #ffd93d; }
    .light:nth-child(5n+3) { background: #6bcb77; color: #6bcb77; }
    .light:nth-child(5n+4) { background: #4d96ff; color: #4d96ff; }
    .light:nth-child(5n+5) { background: #ff6fff; color: #ff6fff; }

    @keyframes twinkle {
      0% { opacity: 0.4; transform: scale(0.9); }
      100% { opacity: 1; transform: scale(1.1); }
    }

    /* Banner festivo */
    .christmas-banner {
      position: fixed;
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%);
      background: linear-gradient(135deg, #c41e3a 0%, #1a472a 50%, #c41e3a 100%);
      background-size: 200% 200%;
      animation: gradientShift 5s ease infinite;
      color: white;
      padding: 12px 40px 12px 20px;
      border-radius: 30px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3), 0 0 30px rgba(255, 215, 0, 0.3);
      z-index: 10001;
      display: flex;
      align-items: center;
      gap: 10px;
      font-family: 'Georgia', serif;
      max-width: 90%;
      border: 2px solid rgba(255, 215, 0, 0.5);
    }

    .christmas-banner.hidden {
      display: none;
    }

    @keyframes gradientShift {
      0% { background-position: 0% 50%; }
      50% { background-position: 100% 50%; }
      100% { background-position: 0% 50%; }
    }

    .banner-content {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .banner-emoji {
      font-size: 1.5rem;
      animation: bounce 1s ease-in-out infinite;
    }

    .banner-emoji:last-child {
      animation-delay: 0.5s;
    }

    @keyframes bounce {
      0%, 100% { transform: translateY(0); }
      50% { transform: translateY(-5px); }
    }

    .banner-text {
      font-size: 0.95rem;
      text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
    }

    .banner-close {
      position: absolute;
      right: 10px;
      top: 50%;
      transform: translateY(-50%);
      background: rgba(255, 255, 255, 0.2);
      border: none;
      color: white;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;
    }

    .banner-close:hover {
      background: rgba(255, 255, 255, 0.4);
      transform: translateY(-50%) scale(1.1);
    }

    /* Botón reactivar */
    .reactivate-btn {
      position: fixed;
      bottom: 20px;
      right: 20px;
      width: 50px;
      height: 50px;
      border-radius: 50%;
      border: none;
      background: linear-gradient(135deg, #c41e3a, #1a472a);
      font-size: 1.5rem;
      cursor: pointer;
      box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
      z-index: 10001;
      transition: all 0.3s;
      animation: pulse 2s ease-in-out infinite;
    }

    .reactivate-btn:hover {
      transform: scale(1.1);
      box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4);
    }

    @keyframes pulse {
      0%, 100% { box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3); }
      50% { box-shadow: 0 4px 25px rgba(255, 215, 0, 0.5); }
    }

    /* Responsive */
    @media (max-width: 768px) {
      .christmas-banner {
        bottom: 10px;
        padding: 10px 35px 10px 15px;
        font-size: 0.85rem;
      }

      .banner-emoji {
        font-size: 1.2rem;
      }

      .banner-text {
        font-size: 0.8rem;
      }

      .snowflake {
        display: none; /* Ocultar copos en móvil por rendimiento */
      }

      .snowflake:nth-child(-n+10) {
        display: block; /* Mostrar solo los primeros 10 */
      }
    }
  `]
})
export class ChristmasDecorationsComponent implements OnInit, OnDestroy {
  themeService = inject(ChristmasThemeService);
  
  snowflakes: Snowflake[] = [];
  lights: number[] = [];
  bannerDismissed = false;

  private snowflakeSymbols = ['❄', '❅', '❆', '✻', '✼', '❉', '✧', '✦'];

  ngOnInit(): void {
    this.generateSnowflakes();
    this.generateLights();
  }

  ngOnDestroy(): void {
    // Cleanup si es necesario
  }

  private generateSnowflakes(): void {
    const count = 30; // Número de copos
    this.snowflakes = Array.from({ length: count }, (_, i) => ({
      id: i,
      left: Math.random() * 100,
      animationDuration: 8 + Math.random() * 12, // 8-20 segundos
      animationDelay: Math.random() * 10, // 0-10 segundos de delay
      size: 10 + Math.random() * 20, // 10-30px
      opacity: 0.4 + Math.random() * 0.6, // 0.4-1.0
      symbol: this.snowflakeSymbols[Math.floor(Math.random() * this.snowflakeSymbols.length)]
    }));
  }

  private generateLights(): void {
    // Generar suficientes luces para cubrir el ancho
    this.lights = Array.from({ length: 40 }, (_, i) => i);
  }

  dismissBanner(): void {
    this.bannerDismissed = true;
    this.themeService.dismiss();
  }
}

