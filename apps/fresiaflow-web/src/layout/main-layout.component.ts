import { Component, OnInit, PLATFORM_ID, Inject, ViewChild } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { ToastModule } from 'primeng/toast';
import { HelpDialogComponent } from '../../ui/components/help-dialog/help-dialog.component';
import { ThemeSelectorComponent } from '../../ui/components/theme-selector/theme-selector.component';
import { FresiaChatComponent } from '../../ui/components/fresia-chat/fresia-chat.component';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    SidebarModule,
    ButtonModule,
    BadgeModule,
    ToastModule,
    HelpDialogComponent,
    ThemeSelectorComponent,
    FresiaChatComponent
  ],
  template: `
    <div class="layout-wrapper">
      <!-- Sidebar Menu -->
      <div class="sidebar-container" [class.collapsed]="!sidebarVisible">
        <!-- Logo removido -->

        <!-- Toggle Button -->
        <button 
          pButton 
          [icon]="sidebarVisible ? 'pi pi-angle-left' : 'pi pi-angle-right'" 
          class="p-button-text toggle-btn"
          (click)="toggleSidebar()">
        </button>

        <!-- User Info -->
        <div class="sidebar-user" *ngIf="sidebarVisible">
          <div class="user-avatar">
            <i class="pi pi-user"></i>
          </div>
          <div class="user-info">
            <div class="user-name">Administrador</div>
            <div class="user-role">Super Usuario</div>
          </div>
        </div>

        <!-- Menu -->
        <div class="sidebar-content">
          <nav class="custom-menu">
            <!-- NAVEGACIÓN -->
            <div class="menu-section">
              <div class="section-header" *ngIf="sidebarVisible">NAVEGACIÓN</div>
              <a routerLink="/dashboard" routerLinkActive="active" class="menu-item">
                <i class="pi pi-home"></i>
                <span *ngIf="sidebarVisible">Dashboard</span>
              </a>
            </div>

            <!-- GESTIÓN -->
            <div class="menu-section">
              <div class="section-header" *ngIf="sidebarVisible">GESTIÓN</div>
              <a routerLink="/import" routerLinkActive="active" class="menu-item">
                <i class="pi pi-download"></i>
                <span *ngIf="sidebarVisible">Importar</span>
              </a>
              <a routerLink="/invoices" routerLinkActive="active" class="menu-item">
                <i class="pi pi-file"></i>
                <span *ngIf="sidebarVisible">Facturas</span>
              </a>
              <a routerLink="/banking" routerLinkActive="active" class="menu-item">
                <i class="pi pi-wallet"></i>
                <span *ngIf="sidebarVisible">Bancos</span>
              </a>
              <a routerLink="/accounting" routerLinkActive="active" class="menu-item">
                <i class="pi pi-calculator"></i>
                <span *ngIf="sidebarVisible">Contabilidad</span>
              </a>
              <a routerLink="/tasks" routerLinkActive="active" class="menu-item">
                <i class="pi pi-check-square"></i>
                <span *ngIf="sidebarVisible">Tareas</span>
              </a>
            </div>

            <!-- SISTEMA -->
            <div class="menu-section">
              <div class="section-header" *ngIf="sidebarVisible">SISTEMA</div>
              <div class="menu-item-wrapper">
                <a (click)="toggleConfig()" class="menu-item clickable" [class.expanded]="configExpanded" [class.active]="isConfigRoute()">
                  <i class="pi pi-cog"></i>
                  <span *ngIf="sidebarVisible">Configuración</span>
                  <i *ngIf="sidebarVisible" class="pi pi-angle-down submenu-icon" [class.rotated]="configExpanded"></i>
                </a>
                
                <div class="submenu" *ngIf="configExpanded && sidebarVisible">
                  <a routerLink="/settings/companies" routerLinkActive="active" [routerLinkActiveOptions]="{exact: false}" class="submenu-item">
                    <i class="pi pi-building"></i>
                    <span>Empresas Propias</span>
                  </a>
                  <a routerLink="/settings/onedrive" routerLinkActive="active" [routerLinkActiveOptions]="{exact: false}" class="submenu-item">
                    <i class="pi pi-microsoft"></i>
                    <span>Sincronización OneDrive</span>
                  </a>
                  <a routerLink="/settings/invoice-sources" routerLinkActive="active" [routerLinkActiveOptions]="{exact: false}" class="submenu-item">
                    <i class="pi pi-cloud-download"></i>
                    <span>Fuentes de Facturas</span>
                  </a>
                  <a routerLink="/settings/accounting" routerLinkActive="active" [routerLinkActiveOptions]="{exact: false}" class="submenu-item">
                    <i class="pi pi-calculator"></i>
                    <span>Contabilidad</span>
                  </a>
                </div>
              </div>
            </div>
          </nav>
        </div>

        <!-- Footer -->
        <div class="sidebar-footer" *ngIf="sidebarVisible">
          <app-theme-selector></app-theme-selector>
          <button pButton label="Ayuda" icon="pi pi-question-circle" class="p-button-text p-button-sm" (click)="showHelp()"></button>
        </div>
        
        <!-- Help Dialog -->
        <app-help-dialog #helpDialog></app-help-dialog>
      </div>

      <!-- Main Content -->
      <div class="main-content" [class.expanded]="!sidebarVisible">
        <div class="content-wrapper">
          <router-outlet></router-outlet>
        </div>
      </div>

      <!-- Chat FresiaFlow Global -->
      <app-fresia-chat></app-fresia-chat>

      <!-- Toast para mensajes globales (errores HTTP, etc.) -->
      <p-toast position="top-right" [life]="5000"></p-toast>
    </div>
  `,
  styles: [`
    .layout-wrapper {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: linear-gradient(135deg, var(--background-color) 0%, var(--secondary-color) 100%);
    }

    .sidebar-container {
      position: fixed;
      left: 0;
      top: 0;
      bottom: 0;
      width: 280px;
      background: linear-gradient(180deg, var(--card-bg) 0%, var(--background-color) 100%);
      border-right: 1px solid var(--secondary-color);
      display: flex;
      flex-direction: column;
      transition: all 0.3s ease;
      z-index: 1000;
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.05);
    }

    .sidebar-container.collapsed {
      width: 70px;
    }


    .toggle-btn {
      position: absolute;
      right: -15px;
      top: 50px;
      z-index: 1001;
      background: var(--card-bg) !important;
      border: 2px solid var(--primary-color) !important;
      border-radius: 50% !important;
      width: 30px !important;
      height: 30px !important;
      padding: 0 !important;
      display: flex !important;
      align-items: center !important;
      justify-content: center !important;
      color: var(--primary-color) !important;
      box-shadow: 0 2px 8px color-mix(in srgb, var(--primary-color) 20%, transparent) !important;
      transition: all 0.3s ease;
    }

    .toggle-btn:hover {
      background: var(--primary-color) !important;
      color: white !important;
      transform: scale(1.1);
    }

    .sidebar-user {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 1.5rem;
      background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-color-strong) 100%);
      color: white;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }

    .sidebar-container.collapsed .sidebar-user {
      padding: 1rem;
      justify-content: center;
    }

    .user-avatar {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.2);
      backdrop-filter: blur(10px);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      flex-shrink: 0;
    }

    .sidebar-container.collapsed .user-avatar {
      width: 40px;
      height: 40px;
    }

    .user-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    .user-name {
      font-weight: 600;
      font-size: 1rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .user-role {
      font-size: 0.75rem;
      opacity: 0.9;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sidebar-content {
      flex: 1;
      padding: 12px 0;
      overflow-y: auto;
      overflow-x: hidden;
    }

    .sidebar-content::-webkit-scrollbar {
      width: 6px;
    }

    .sidebar-content::-webkit-scrollbar-track {
      background: transparent;
    }

    .sidebar-content::-webkit-scrollbar-thumb {
      background: var(--secondary-color);
      border-radius: 3px;
    }

    .sidebar-content::-webkit-scrollbar-thumb:hover {
      background: #d1d5db;
      opacity: 0.5;
    }

    .custom-menu {
      width: 100%;
    }

    .menu-section {
      margin-bottom: 8px;
    }

    .section-header {
      font-weight: 600;
      color: #6b7280;
      opacity: 0.7;
      padding: 12px 16px;
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-top: 8px;
    }

    .menu-item {
      display: flex;
      align-items: center;
      gap: 12px;
      border-radius: 8px;
      margin: 4px 8px;
      padding: 12px 16px;
      transition: all 0.3s ease;
      color: var(--text-color);
      text-decoration: none;
      cursor: pointer;
      position: relative;
    }

    .menu-item.clickable {
      justify-content: space-between;
    }

    .menu-item:hover {
      background: var(--secondary-color);
      color: var(--primary-color);
      transform: translateX(4px);
    }

    .menu-item.active {
      background: var(--secondary-color);
      color: var(--primary-color);
      font-weight: 600;
    }

    .menu-item.active i {
      color: var(--primary-color);
    }

    .menu-item i:first-child {
      color: var(--primary-color);
      font-size: 1.1rem;
      min-width: 20px;
    }

    .submenu-icon {
      margin-left: auto;
      font-size: 0.875rem;
      transition: transform 0.3s ease;
      color: var(--primary-color);
    }

    .submenu-icon.rotated {
      transform: rotate(180deg);
    }

    .submenu {
      padding-left: 28px;
      border-left: 2px solid var(--secondary-color);
      margin-left: 8px;
      margin-top: 4px;
    }

    .submenu-item {
      display: flex;
      align-items: center;
      gap: 12px;
      border-radius: 8px;
      margin: 2px 8px;
      padding: 10px 16px;
      transition: all 0.3s ease;
      color: var(--text-color);
      text-decoration: none;
      font-size: 0.9rem;
    }

    .submenu-item:hover {
      background: var(--secondary-color);
      color: var(--primary-color);
      transform: translateX(4px);
    }

    .submenu-item.active {
      background: var(--secondary-color);
      color: var(--primary-color);
      font-weight: 600;
    }

    .submenu-item.active i {
      color: var(--primary-color);
    }

    .submenu-item i {
      color: var(--primary-color);
      font-size: 1rem;
    }

    .sidebar-container.collapsed .menu-item {
      justify-content: center;
      padding: 12px 8px;
    }

    .sidebar-container.collapsed .menu-item i:first-child {
      font-size: 1.3rem;
    }

    .sidebar-container.collapsed .menu-item:hover {
      transform: scale(1.05);
    }

    .sidebar-footer {
      padding: 16px;
      border-top: 1px solid var(--secondary-color);
      background: var(--background-color);
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .main-content {
      flex: 1;
      margin-left: 280px;
      overflow-y: auto;
      background: linear-gradient(135deg, var(--background-color) 0%, var(--secondary-color) 100%);
      transition: margin-left 0.3s ease;
      min-height: 100vh;
    }

    .main-content.expanded {
      margin-left: 70px;
    }

    .content-wrapper {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    @media (max-width: 768px) {
      .sidebar-container {
        transform: translateX(-100%);
      }

      .sidebar-container:not(.collapsed) {
        transform: translateX(0);
      }

      .main-content {
        margin-left: 0;
      }

      .toggle-btn {
        right: auto;
        left: 10px;
        top: 10px;
      }

      .content-wrapper {
        padding: 1rem;
      }
    }
  `]
})
export class MainLayoutComponent implements OnInit {
  sidebarVisible = true;
  configExpanded = false;
  @ViewChild('helpDialog') helpDialog!: HelpDialogComponent;

  constructor(
    @Inject(PLATFORM_ID) private platformId: Object,
    private router: Router
  ) {}

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.sidebarVisible = window.innerWidth >= 768;
    }

    // Detectar si estamos en una ruta de configuración y expandir el menú
    this.checkConfigRoute();
    
    // Escuchar cambios de ruta
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.checkConfigRoute();
    });
  }

  private checkConfigRoute(): void {
    const url = this.router.url;
    if (url.includes('/settings/')) {
      this.configExpanded = true;
    }
  }

  isConfigRoute(): boolean {
    return this.router.url.includes('/settings/');
  }

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }

  toggleConfig() {
    this.configExpanded = !this.configExpanded;
  }

  showHelp() {
    if (this.helpDialog) {
      this.helpDialog.show();
    }
  }
}
