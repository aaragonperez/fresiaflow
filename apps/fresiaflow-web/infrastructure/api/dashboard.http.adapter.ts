import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DashboardApiPort } from '../../ports/dashboard.api.port';
import { DashboardTask, BankSummary, Alert } from '../../domain/dashboard.model';
import { firstValueFrom } from 'rxjs';

/**
 * Adapter HTTP para el puerto del Dashboard.
 * Implementa la comunicación con el backend para obtener datos del dashboard.
 */
@Injectable({ providedIn: 'root' })
export class DashboardHttpAdapter implements DashboardApiPort {
  private readonly baseUrl = '/api/dashboard';

  constructor(private http: HttpClient) {}

  async getTasks(): Promise<DashboardTask[]> {
    try {
      const response = await firstValueFrom(
        this.http.get<DashboardTaskDto[]>(`${this.baseUrl}/tasks`)
      );
      return response.map(this.mapTaskToDomain);
    } catch (error: any) {
      // Si el endpoint no existe aún, retornar array vacío
      if (error.status === 404) {
        console.warn('Dashboard tasks endpoint not implemented yet');
        return [];
      }
      throw error;
    }
  }

  async getBankBalances(): Promise<BankSummary> {
    try {
      const response = await firstValueFrom(
        this.http.get<BankSummaryDto>(`${this.baseUrl}/bank-balances`)
      );
      return this.mapBankSummaryToDomain(response);
    } catch (error: any) {
      // Si el endpoint no existe aún, retornar estructura vacía
      if (error.status === 404) {
        console.warn('Dashboard bank balances endpoint not implemented yet');
        return {
          banks: [],
          totalBalance: 0,
          primaryCurrency: 'EUR'
        };
      }
      throw error;
    }
  }

  async getAlerts(): Promise<Alert[]> {
    try {
      const response = await firstValueFrom(
        this.http.get<AlertDto[]>(`${this.baseUrl}/alerts`)
      );
      return response.map(this.mapAlertToDomain);
    } catch (error: any) {
      // Si el endpoint no existe aún, retornar array vacío
      if (error.status === 404) {
        console.warn('Dashboard alerts endpoint not implemented yet');
        return [];
      }
      throw error;
    }
  }

  private mapTaskToDomain(dto: DashboardTaskDto): DashboardTask {
    return {
      id: dto.id,
      title: dto.title,
      description: dto.description,
      type: dto.type as any,
      priority: dto.priority as any,
      status: dto.status as any,
      dueDate: dto.dueDate ? new Date(dto.dueDate) : undefined,
      createdAt: new Date(dto.createdAt),
      updatedAt: new Date(dto.updatedAt),
      metadata: dto.metadata
    };
  }

  private mapBankSummaryToDomain(dto: BankSummaryDto): BankSummary {
    return {
      banks: dto.banks.map(bank => ({
        bankId: bank.bankId,
        bankName: bank.bankName,
        accountNumber: bank.accountNumber,
        balance: bank.balance,
        currency: bank.currency,
        lastMovementDate: bank.lastMovementDate ? new Date(bank.lastMovementDate) : undefined,
        lastMovementAmount: bank.lastMovementAmount
      })),
      totalBalance: dto.totalBalance,
      primaryCurrency: dto.primaryCurrency,
      previousDayBalance: dto.previousDayBalance,
      previousDayVariation: dto.previousDayVariation,
      previousMonthBalance: dto.previousMonthBalance,
      previousMonthVariation: dto.previousMonthVariation
    };
  }

  private mapAlertToDomain(dto: AlertDto): Alert {
    return {
      id: dto.id,
      type: dto.type as any,
      severity: dto.severity as any,
      title: dto.title,
      description: dto.description,
      occurredAt: new Date(dto.occurredAt),
      acknowledgedAt: dto.acknowledgedAt ? new Date(dto.acknowledgedAt) : undefined,
      resolvedAt: dto.resolvedAt ? new Date(dto.resolvedAt) : undefined,
      metadata: dto.metadata
    };
  }
}

// DTOs para mapeo desde el backend
interface DashboardTaskDto {
  id: string;
  title: string;
  description?: string;
  type: string;
  priority: string;
  status: string;
  dueDate?: string;
  createdAt: string;
  updatedAt: string;
  metadata?: Record<string, any>;
}

interface BankSummaryDto {
  banks: Array<{
    bankId: string;
    bankName: string;
    accountNumber?: string;
    balance: number;
    currency: string;
    lastMovementDate?: string;
    lastMovementAmount?: number;
  }>;
  totalBalance: number;
  primaryCurrency: string;
  previousDayBalance?: number;
  previousDayVariation?: number;
  previousMonthBalance?: number;
  previousMonthVariation?: number;
}

interface AlertDto {
  id: string;
  type: string;
  severity: string;
  title: string;
  description: string;
  occurredAt: string;
  acknowledgedAt?: string;
  resolvedAt?: string;
  metadata?: Record<string, any>;
}

