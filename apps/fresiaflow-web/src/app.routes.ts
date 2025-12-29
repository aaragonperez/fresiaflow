import { Routes } from '@angular/router';
import { TasksPageComponent } from '../ui/pages/tasks-page/tasks-page.component';
import { InvoicesPageComponent } from '../ui/pages/invoices-page/invoices-page.component';
import { ImportPageComponent } from '../ui/pages/import-page/import-page.component';
import { SettingsPageComponent } from '../ui/pages/settings-page/settings-page.component';
import { DashboardPageComponent } from '../ui/pages/dashboard-page/dashboard-page.component';
import { AccountingPageComponent } from '../ui/pages/accounting-page/accounting-page.component';
import { BankingPageComponent } from '../ui/pages/banking-page/banking-page.component';

export const routes: Routes = [
  { path: 'dashboard', component: DashboardPageComponent },
  { path: 'import', component: ImportPageComponent },
  { path: 'invoices', component: InvoicesPageComponent },
  { path: 'banking', component: BankingPageComponent },
  { path: 'accounting', component: AccountingPageComponent },
  { path: 'tasks', component: TasksPageComponent },
  { path: 'settings/companies', component: SettingsPageComponent },
  { path: 'settings/onedrive', component: SettingsPageComponent },
  { path: 'settings/invoice-sources', component: SettingsPageComponent },
  { path: 'settings/accounting', component: SettingsPageComponent },
  { path: 'settings', redirectTo: '/settings/companies', pathMatch: 'full' },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' }
];

