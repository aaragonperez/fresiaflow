import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BankSummary } from '../../../domain/dashboard.model';

/**
 * Componente widget para mostrar el resumen de saldos bancarios.
 * Muestra lista de bancos, saldos y variaciones.
 */
@Component({
  selector: 'app-bank-summary-widget',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bank-summary-widget.component.html',
  styleUrls: ['./bank-summary-widget.component.css']
})
export class BankSummaryWidgetComponent {
  @Input() bankSummary: BankSummary | null = null;
  @Input() loading = false;
  @Input() error: string | null = null;

  /**
   * Formatea un importe monetario.
   */
  formatCurrency(amount: number, currency: string = 'EUR'): string {
    return new Intl.NumberFormat('es-ES', {
      style: 'currency',
      currency: currency
    }).format(amount);
  }

  /**
   * Formatea una variación porcentual.
   */
  formatVariation(variation: number | undefined): string {
    if (variation === undefined || variation === null) return '';
    const sign = variation >= 0 ? '+' : '';
    return `${sign}${variation.toFixed(2)}%`;
  }

  /**
   * Obtiene la clase CSS según el signo de la variación.
   */
  getVariationClass(variation: number | undefined): string {
    if (variation === undefined || variation === null) return '';
    return variation >= 0 ? 'positive' : 'negative';
  }

  /**
   * Formatea una fecha para mostrar.
   */
  formatDate(date: Date | undefined): string {
    if (!date) return 'N/A';
    const d = new Date(date);
    return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}

