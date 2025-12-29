import { Component, OnInit, signal, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { AccountingService, AccountingAccount } from '../../../infrastructure/services/accounting.service';

@Component({
  selector: 'app-accounting-accounts-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    DialogModule,
    DropdownModule,
    TagModule
  ],
  templateUrl: './accounting-accounts-config.component.html',
  styleUrls: ['./accounting-accounts-config.component.css']
})
export class AccountingAccountsConfigComponent implements OnInit {
  @ViewChild('dt') table!: Table;

  service = inject(AccountingService);

  accounts = signal<AccountingAccount[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Diálogo de edición
  editDialogVisible = signal(false);
  editingAccount = signal<AccountingAccount | null>(null);
  accountCode = signal('');
  accountName = signal('');
  accountType = signal<'Asset' | 'Liability' | 'Equity' | 'Income' | 'Expense'>('Expense');
  isActive = signal(true);

  // Filtros
  globalFilter = signal('');

  accountTypeOptions = [
    { label: 'Activo', value: 'Asset' },
    { label: 'Pasivo', value: 'Liability' },
    { label: 'Patrimonio Neto', value: 'Equity' },
    { label: 'Ingresos', value: 'Income' },
    { label: 'Gastos', value: 'Expense' }
  ];

  ngOnInit(): void {
    this.loadAccounts();
  }

  async loadAccounts(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.service.loadAccounts();
      this.accounts.set(this.service.accounts());
    } catch (err: any) {
      this.error.set(err.error?.error || err.message || 'Error al cargar cuentas contables');
    } finally {
      this.loading.set(false);
    }
  }

  openNewDialog(): void {
    this.editingAccount.set(null);
    this.accountCode.set('');
    this.accountName.set('');
    this.accountType.set('Expense');
    this.isActive.set(true);
    this.editDialogVisible.set(true);
  }

  openEditDialog(account: AccountingAccount): void {
    this.editingAccount.set(account);
    this.accountCode.set(account.code);
    this.accountName.set(account.name);
    this.accountType.set(account.type);
    this.isActive.set(account.isActive);
    this.editDialogVisible.set(true);
  }

  closeEditDialog(): void {
    this.editDialogVisible.set(false);
    this.editingAccount.set(null);
    this.accountCode.set('');
    this.accountName.set('');
    this.accountType.set('Expense');
    this.isActive.set(true);
  }

  async saveAccount(): Promise<void> {
    if (!this.accountCode().trim() || !this.accountName().trim()) {
      alert('El código y el nombre son obligatorios');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    try {
      const accountData = {
        id: this.editingAccount()?.id,
        code: this.accountCode().trim(),
        name: this.accountName().trim(),
        type: this.accountType(),
        isActive: this.isActive()
      };

      await this.service.createOrUpdateAccount(accountData);
      this.closeEditDialog();
      await this.loadAccounts();
    } catch (err: any) {
      this.error.set(err.error?.error || err.message || 'Error al guardar cuenta contable');
    } finally {
      this.loading.set(false);
    }
  }

  async deleteAccount(account: AccountingAccount): Promise<void> {
    if (!confirm(`¿Eliminar la cuenta "${account.code} - ${account.name}"?`)) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    try {
      await this.service.deleteAccount(account.id);
      await this.loadAccounts();
    } catch (err: any) {
      this.error.set(err.error?.error || err.message || 'Error al eliminar cuenta contable');
    } finally {
      this.loading.set(false);
    }
  }

  getAccountTypeLabel(type: string): string {
    const option = this.accountTypeOptions.find(opt => opt.value === type);
    return option?.label || type;
  }

  getAccountTypeSeverity(type: string): string {
    switch (type) {
      case 'Asset': return 'info';
      case 'Liability': return 'warning';
      case 'Equity': return 'success';
      case 'Income': return 'success';
      case 'Expense': return 'danger';
      default: return 'info';
    }
  }
}

