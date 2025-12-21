import { Component, OnInit, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { BadgeModule } from 'primeng/badge';
import { MenuItem } from 'primeng/api';

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
    BadgeModule
  ],
  template: `
    <div class="layout-wrapper">
      <!-- Sidebar Menu -->
      <div class="sidebar-container" [class.collapsed]="!sidebarVisible">
        <!-- Logo -->
        <div class="sidebar-logo">
          <img src="/assets/fresiaflow-logo.png" alt="FresiaFlow" class="logo-image" />
        </div>

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
          <button pButton label="Ayuda" icon="pi pi-question-circle" class="p-button-text p-button-sm"></button>
        </div>
      </div>

      <!-- Main Content -->
      <div class="main-content" [class.expanded]="!sidebarVisible">
        <div class="content-wrapper">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .layout-wrapper {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: linear-gradient(135deg, #fafafa 0%, #f5f5f5 100%);
    }

    .sidebar-container {
      position: fixed;
      left: 0;
      top: 0;
      bottom: 0;
      width: 280px;
      background: linear-gradient(180deg, #ffffff 0%, #fafafa 100%);
      border-right: 1px solid #e5e7eb;
      display: flex;
      flex-direction: column;
      transition: all 0.3s ease;
      z-index: 1000;
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.05);
    }

    .sidebar-container.collapsed {
      width: 70px;
    }

    .sidebar-logo {
      padding: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      border-bottom: 2px solid #fee2e2;
      background: linear-gradient(135deg, #ffffff 0%, #fef2f2 100%);
      position: relative;
      min-height: 120px;
    }

    .logo-image {
      width: 100%;
      height: 100%;
      object-fit: contain;
      transition: all 0.3s ease;
    }

    .sidebar-container.collapsed .logo-image {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }

    .toggle-btn {
      position: absolute;
      right: -15px;
      top: 80px;
      z-index: 1001;
      background: white !important;
      border: 2px solid #dc2626 !important;
      border-radius: 50% !important;
      width: 30px !important;
      height: 30px !important;
      padding: 0 !important;
      display: flex !important;
      align-items: center !important;
      justify-content: center !important;
      color: #dc2626 !important;
      box-shadow: 0 2px 8px rgba(220, 38, 38, 0.2) !important;
      transition: all 0.3s ease;
    }

    .toggle-btn:hover {
      background: #dc2626 !important;
      color: white !important;
      transform: scale(1.1);
    }

    .sidebar-user {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 1.5rem;
      background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
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
      background: #e5e7eb;
      border-radius: 3px;
    }

    .sidebar-content::-webkit-scrollbar-thumb:hover {
      background: #d1d5db;
    }

    ::ng-deep .sidebar-container .p-menu {
      background: transparent;
    }

    ::ng-deep .sidebar-container .p-menuitem-link {
      border-radius: 8px;
      margin: 4px 8px;
      padding: 12px 16px;
      transition: all 0.3s ease;
      color: #1f2937;
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
      background: #fee2e2;
      color: #b91c1c;
      transform: translateX(4px);
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-link:hover {
      transform: translateX(0) scale(1.05);
    }

    ::ng-deep .sidebar-container .p-menuitem-icon {
      color: #dc2626;
      margin-right: 12px;
      font-size: 1.1rem;
    }

    ::ng-deep .sidebar-container.collapsed .p-menuitem-icon {
      margin-right: 0;
      font-size: 1.3rem;
    }

    ::ng-deep .sidebar-container .p-submenu-header {
      font-weight: 600;
      color: #6b7280;
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
      border-top: 1px solid #e5e7eb;
      background: #fafafa;
    }

    .main-content {
      flex: 1;
      margin-left: 280px;
      overflow-y: auto;
      background: linear-gradient(135deg, #f9fafb 0%, #f3f4f6 100%);
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

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.sidebarVisible = window.innerWidth >= 768;
    }
  }

  toggleSidebar() {
    this.sidebarVisible = !this.sidebarVisible;
  }

  menuItems: MenuItem[] = [
    {
      label: 'NAVEGACIÓN',
      items: [
        {
          label: 'Dashboard',
          icon: 'pi pi-home',
          routerLink: '/tasks'
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
      routerLink: '/tasks',
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
