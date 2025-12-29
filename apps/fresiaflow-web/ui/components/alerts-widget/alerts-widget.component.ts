import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginatorModule } from 'primeng/paginator';
import { Alert, AlertSeverity, AlertType } from '../../../domain/dashboard.model';

/**
 * Componente widget para mostrar alertas financieras y del sistema.
 * Muestra alertas ordenadas por severidad.
 */
@Component({
  selector: 'app-alerts-widget',
  standalone: true,
  imports: [CommonModule, PaginatorModule],
  templateUrl: './alerts-widget.component.html',
  styleUrls: ['./alerts-widget.component.css']
})
export class AlertsWidgetComponent {
  @Input() alerts: Alert[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;

  // Paginación
  currentPage = signal(0);
  rowsPerPage = signal(5);
  rowsPerPageOptions = [5, 10, 20, 50];

  // Enums para usar en el template
  readonly AlertSeverity = AlertSeverity;
  readonly AlertType = AlertType;

  /**
   * Obtiene el icono según el tipo de alerta.
   */
  getAlertTypeIcon(type: AlertType): string {
    switch (type) {
      case AlertType.UnusualMovement:
        return 'pi pi-exclamation-triangle';
      case AlertType.DuplicateAmount:
        return 'pi pi-copy';
      case AlertType.UnidentifiedCharge:
        return 'pi pi-question-circle';
      case AlertType.PatternDeviation:
        return 'pi pi-chart-line';
      case AlertType.MissingSupplier:
        return 'pi pi-building';
      case AlertType.OverdueInvoice:
        return 'pi pi-file';
      case AlertType.LowBalance:
        return 'pi pi-wallet';
      case AlertType.System:
        return 'pi pi-cog';
      default:
        return 'pi pi-info-circle';
    }
  }

  /**
   * Obtiene la clase CSS según la severidad.
   */
  getSeverityClass(severity: AlertSeverity): string {
    switch (severity) {
      case AlertSeverity.Critical:
        return 'severity-critical';
      case AlertSeverity.High:
        return 'severity-high';
      case AlertSeverity.Medium:
        return 'severity-medium';
      case AlertSeverity.Low:
        return 'severity-low';
      case AlertSeverity.Info:
        return 'severity-info';
      default:
        return '';
    }
  }

  /**
   * Obtiene el texto de la severidad en español.
   */
  getSeverityText(severity: AlertSeverity): string {
    switch (severity) {
      case AlertSeverity.Critical:
        return 'Crítica';
      case AlertSeverity.High:
        return 'Alta';
      case AlertSeverity.Medium:
        return 'Media';
      case AlertSeverity.Low:
        return 'Baja';
      case AlertSeverity.Info:
        return 'Información';
      default:
        return severity;
    }
  }

  /**
   * Obtiene el texto del tipo de alerta en español.
   */
  getAlertTypeText(type: AlertType): string {
    switch (type) {
      case AlertType.UnusualMovement:
        return 'Movimiento Inusual';
      case AlertType.DuplicateAmount:
        return 'Importe Duplicado';
      case AlertType.UnidentifiedCharge:
        return 'Cargo Sin Identificar';
      case AlertType.PatternDeviation:
        return 'Desviación de Patrón';
      case AlertType.MissingSupplier:
        return 'Proveedor Faltante';
      case AlertType.OverdueInvoice:
        return 'Factura Vencida';
      case AlertType.LowBalance:
        return 'Saldo Bajo';
      case AlertType.System:
        return 'Sistema';
      default:
        return type;
    }
  }

  /**
   * Verifica si una alerta está resuelta.
   */
  isResolved(alert: Alert): boolean {
    return !!alert.resolvedAt;
  }

  /**
   * Obtiene el número de alertas no resueltas.
   */
  get unresolvedAlertsCount(): number {
    return this.alerts.filter(a => !a.resolvedAt).length;
  }

  /**
   * Obtiene las alertas paginadas.
   */
  get paginatedAlerts(): Alert[] {
    const start = this.currentPage() * this.rowsPerPage();
    const end = start + this.rowsPerPage();
    return this.alerts.slice(start, end);
  }

  /**
   * Total de alertas (para el paginador).
   */
  get totalAlerts(): number {
    return this.alerts.length;
  }

  /**
   * Maneja el cambio de página en el paginador.
   */
  onPageChange(event: any): void {
    this.currentPage.set(event.page);
    this.rowsPerPage.set(event.rows);
  }

  /**
   * Verifica si hay alertas críticas sin resolver.
   */
  get hasCriticalAlerts(): boolean {
    return this.alerts.some(a => a.severity === AlertSeverity.Critical && !a.resolvedAt);
  }

  /**
   * Formatea una fecha para mostrar.
   */
  formatDate(date: Date): string {
    const d = new Date(date);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Ahora';
    if (diffMins < 60) return `Hace ${diffMins} min`;
    if (diffHours < 24) return `Hace ${diffHours} h`;
    if (diffDays < 7) return `Hace ${diffDays} días`;
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' });
  }
}

