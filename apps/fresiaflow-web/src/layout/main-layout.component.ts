import { Component, OnInit, PLATFORM_ID, Inject, ViewChild } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { BadgeModule } from 'primeng/badge';
import { MenuItem } from 'primeng/api';
import { HelpDialogComponent } from '../../ui/components/help-dialog/help-dialog.component';
import { ThemeSelectorComponent } from '../../ui/components/theme-selector/theme-selector.component';
import { FresiaChatComponent } from '../../ui/components/fresia-chat/fresia-chat.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    SidebarModule,
    ButtonModule,
    MenuModule,
    BadgeModule,
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
          <p-menu 
            [model]="sidebarVisible ? menuItems : menuItemsCollapsed" 
            [style]="{width: '100%', border: 'none'}">
          </p-menu>
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
    </div>
  `,
  styles: [`
    .layout-wrapper {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: linear-gradient(135deg, var(--background-color, #fafafa) 0%, var(--secondary-color, #f5f5f5) 100%);
    }

    .sidebar-container {
      position: fixed;
      left: 0;
      top: 0;
      bottom: 0;
      width: 280px;
      background: linear-gradient(180deg, var(--card-bg, #ffffff) 0%, var(--background-color, #fafafa) 100%);
      border-right: 1px solid var(--secondary-color, #e5e7eb);
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
      background: var(--card-bg, white) !important;
      border: 2px solid var(--primary-color, #dc2626) !important;
      border-radius: 50% !important;
      width: 30px !important;
      height: 30px !important;
      padding: 0 !important;
      display: flex !important;
      align-items: center !important;
      justify-content: center !important;
      color: var(--primary-color, #dc2626) !important;
      box-shadow: 0 2px 8px color-mix(in srgb, var(--primary-color, #dc2626) 20%, transparent) !important;
      transition: all 0.3s ease;
    }

    .toggle-btn:hover {
      background: var(--primary-color, #dc2626) !important;
      color: white !important;
      transform: scale(1.1);
    }

    .sidebar-user {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 1.5rem;
      background: linear-gradient(135deg, var(--primary-color, #dc2626) 0%, var(--primary-color, #b91c1c) 100%);
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
      background: var(--secondary-color, #e5e7eb);
      border-radius: 3px;
    }

    .sidebar-content::-webkit-scrollbar-thumb:hover {
      background: var(--text-color, #d1d5db);
      opacity: 0.5;
    }

    ::ng-deep .sidebar-container .p-menu {
      background: transparent;
    }

    ::ng-deep .sidebar-container .p-menuitem-link {
      border-radius: 8px;
      margin: 4px 8px;
      padding: 12px 16px;
      transition: all 0.3s ease;
      color: var(--text-color, #1f2937);
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-link {
      justify-content: center;
      padding: 12px 8px;
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-text,
    ::ng-deep .sidebar-container.collapsed .p-submenu-icon {
      display: none;
    }

    ::ng-deep .sidebar-container .p-menuitem-link:hover {
      background: var(--secondary-color, #fee2e2);
      color: var(--primary-color, #b91c1c);
      transform: translateX(4px);
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-link:hover {
      transform: translateX(0) scale(1.05);
    }

    ::ng-deep .sidebar-container .p-menuitem-icon {
      color: var(--primary-color, #dc2626);
      margin-right: 12px;
      font-size: 1.1rem;
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-icon {
      margin-right: 0;
      font-size: 1.3rem;
    }

    ::ng-deep .sidebar-container .p-submenu-header {
      font-weight: 600;
      color: var(--text-color, #6b7280);
      opacity: 0.7;
      padding: 12px 16px;
      font-size: 0.7rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-top: 8px;
    }

    ::ng-deep .sidebar-container.collapsed .p-submenu-header {
      display: none;
    }

    .sidebar-footer {
      padding: 16px;
      border-top: 1px solid var(--secondary-color, #e5e7eb);
      background: var(--background-color, #fafafa);
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .main-content {
      flex: 1;
      margin-left: 280px;
      overflow-y: auto;
      background: linear-gradient(135deg, var(--background-color, #f9fafb) 0%, var(--secondary-color, #f3f4f6) 100%);
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
  @ViewChild('helpDialog') helpDialog!: HelpDialogComponent;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.sidebarVisible = window.innerWidth >= 768;
    }
  }

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }

  showHelp() {
    if (this.helpDialog) {
      this.helpDialog.show();
    }
  }

  menuItems: MenuItem[] = [
    {
      label: 'NAVEGACIÓN',
      items: [
        {
          label: 'Dashboard',
          icon: 'pi pi-home',
          routerLink: '/dashboard'
        }
      ]
    },
    {
      label: 'GESTIÓN',
      items: [
        {
          label: 'Tareas',
          icon: 'pi pi-check-square',
          routerLink: '/tasks'
        },
        {
          label: 'Facturas',
          icon: 'pi pi-file',
          routerLink: '/invoices'
        },
        {
          label: 'Bancos',
          icon: 'pi pi-wallet',
          routerLink: '/banking'
        }
      ]
    },
    {
      label: 'SISTEMA',
      items: [
        {
          label: 'Configuración',
          icon: 'pi pi-cog',
          routerLink: '/settings'
        }
      ]
    }
  ];

  menuItemsCollapsed: MenuItem[] = [
    {
      label: 'Dashboard',
      icon: 'pi pi-home',
      routerLink: '/dashboard',
      title: 'Dashboard'
    },
    {
      label: 'Tareas',
      icon: 'pi pi-check-square',
      routerLink: '/tasks',
      title: 'Tareas'
    },
    {
      label: 'Facturas',
      icon: 'pi pi-file',
      routerLink: '/invoices',
      title: 'Facturas'
    },
    {
      label: 'Bancos',
      icon: 'pi pi-wallet',
      routerLink: '/banking',
      title: 'Bancos'
    },
    {
      label: 'Configuración',
      icon: 'pi pi-cog',
      routerLink: '/settings',
      title: 'Configuración'
    }
  ];
}
