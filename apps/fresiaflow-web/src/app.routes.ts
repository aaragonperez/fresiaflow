import { Routes } from '@angular/router';
import { TasksPageComponent } from '../ui/pages/tasks-page/tasks-page.component';
import { InvoicesPageComponent } from '../ui/pages/invoices-page/invoices-page.component';
import { SettingsPageComponent } from '../ui/pages/settings-page/settings-page.component';
import { DashboardPageComponent } from '../ui/pages/dashboard-page/dashboard-page.component';

export const routes: Routes = [
  { path: 'dashboard', component: DashboardPageComponent },
  { path: 'tasks', component: TasksPageComponent },
  { path: 'invoices', component: InvoicesPageComponent },
  { path: 'settings', component: SettingsPageComponent },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' }
];

