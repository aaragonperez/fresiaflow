import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { DialogModule } from 'primeng/dialog';
import { 
  OneDriveSyncService, 
  OneDriveSyncConfigUpdate
} from '../../../infrastructure/services/onedrive-sync.service';

@Component({
  selector: 'app-onedrive-config',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    InputNumberModule,
    ButtonModule,
    InputSwitchModule,
    ProgressSpinnerModule,
    MessageModule,
    DialogModule
  ],
  templateUrl: './onedrive-config.component.html',
  styleUrl: './onedrive-config.component.css'
})
export class OneDriveConfigComponent implements OnInit {
  private syncService = inject(OneDriveSyncService);

  // Form fields
  enabled = signal(false);
  tenantId = signal('');
  clientId = signal('');
  clientSecret = signal('');
  folderPath = signal('');
  driveId = signal('');
  syncIntervalMinutes = signal(15);

  // State
  saveSuccess = signal(false);
  
  // Collapsible sections state
  azureConfigExpanded = signal(true);
  folderConfigExpanded = signal(true);
  automaticSyncExpanded = signal(true);
  dangerZoneExpanded = signal(false);
  instructionsExpanded = signal(false);

  // Clear database dialog
  clearDialogVisible = signal(false);
  clearConfirmCode = signal('');
  clearingDatabase = signal(false);
  clearResult = signal<{ success: boolean; message: string } | null>(null);

  // Expose service signals
  config = this.syncService.config;
  loading = this.syncService.loading;
  error = this.syncService.error;

  ngOnInit(): void {
    this.loadConfig();
  }

  async loadConfig(): Promise<void> {
    await this.syncService.loadConfig();

    const cfg = this.config();
    if (cfg) {
      this.enabled.set(cfg.enabled);
      this.tenantId.set(cfg.tenantId || '');
      this.clientId.set(cfg.clientId || '');
      this.clientSecret.set(''); // Nunca pre-popular el secret por seguridad
      this.folderPath.set(cfg.folderPath || '');
      this.driveId.set(cfg.driveId || '');
      this.syncIntervalMinutes.set(cfg.syncIntervalMinutes);
    }
  }

  toggleAzureConfig(): void {
    this.azureConfigExpanded.set(!this.azureConfigExpanded());
  }

  toggleFolderConfig(): void {
    this.folderConfigExpanded.set(!this.folderConfigExpanded());
  }

  toggleAutomaticSync(): void {
    this.automaticSyncExpanded.set(!this.automaticSyncExpanded());
  }

  toggleInstructions(): void {
    this.instructionsExpanded.set(!this.instructionsExpanded());
  }

  toggleDangerZone(): void {
    this.dangerZoneExpanded.set(!this.dangerZoneExpanded());
  }

  openClearDialog(): void {
    this.clearConfirmCode.set('');
    this.clearResult.set(null);
    this.clearDialogVisible.set(true);
  }

  closeClearDialog(): void {
    this.clearDialogVisible.set(false);
    this.clearConfirmCode.set('');
  }

  async clearDatabase(): Promise<void> {
    if (this.clearConfirmCode() !== 'DELETE_ALL_DATA') {
      this.clearResult.set({
        success: false,
        message: 'El código de confirmación no es correcto. Escribe DELETE_ALL_DATA exactamente.'
      });
      return;
    }

    this.clearingDatabase.set(true);
    this.clearResult.set(null);

    try {
      const result = await this.syncService.clearDatabase(this.clearConfirmCode());
      this.clearResult.set({
        success: result.success,
        message: result.message
      });
      
      if (result.success) {
        // Recargar la configuración
        await this.loadConfig();
        
        // Cerrar el diálogo después de 2 segundos
        setTimeout(() => {
          this.closeClearDialog();
        }, 2000);
      }
    } catch (err: any) {
      this.clearResult.set({
        success: false,
        message: err.message || 'Error al vaciar la base de datos'
      });
    } finally {
      this.clearingDatabase.set(false);
    }
  }

  async saveConfig(): Promise<void> {
    this.saveSuccess.set(false);

    try {
      const config: OneDriveSyncConfigUpdate = {
        enabled: this.enabled(),
        tenantId: this.tenantId(),
        clientId: this.clientId(),
        clientSecret: this.clientSecret(),
        folderPath: this.folderPath(),
        driveId: this.driveId(),
        syncIntervalMinutes: this.syncIntervalMinutes()
      };

      await this.syncService.saveConfig(config);
      this.saveSuccess.set(true);

      // Limpiar el client secret después de guardar
      this.clientSecret.set('');

      setTimeout(() => this.saveSuccess.set(false), 5000);
    } catch (err: any) {
      console.error('Error guardando configuración:', err);
    }
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString();
  }
}

