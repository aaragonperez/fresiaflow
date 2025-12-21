import { Routes } from '@angular/router';
import { TasksPageComponent } from '../ui/pages/tasks-page/tasks-page.component';
import { InvoicesPageComponent } from '../ui/pages/invoices-page/invoices-page.component';

export const routes: Routes = [
  { path: 'tasks', component: TasksPageComponent },
  { path: 'invoices', component: InvoicesPageComponent },
  { path: '', redirectTo: '/tasks', pathMatch: 'full' }
];

