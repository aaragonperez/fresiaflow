import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';
import { DividerModule } from 'primeng/divider';
import { TagModule } from 'primeng/tag';
import { TabViewModule } from 'primeng/tabview';
import { ImageModule } from 'primeng/image';

@Component({
  selector: 'app-help-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    AccordionModule,
    DividerModule,
    TagModule,
    TabViewModule,
    ImageModule
  ],
  template: `
    <p-dialog 
      [(visible)]="visible" 
      [modal]="true" 
      [style]="{width: '1000px', maxWidth: '95vw'}"
      [draggable]="false"
      [resizable]="false"
      (onHide)="onClose()">
      
      <ng-template pTemplate="header">
        <div class="dialog-header">
          <i class="pi pi-question-circle"></i>
          <span>Ayuda de FresiaFlow</span>
          <span class="version-badge">v1.1.0</span>
        </div>
      </ng-template>
      
      <div class="help-content">
        <p-tabView>
          <!-- Tab: Inicio -->
          <p-tabPanel header="🏠 Inicio">
            <div class="help-section">
              <h2><i class="pi pi-info-circle"></i> ¿Qué es FresiaFlow?</h2>
              <p>
                FresiaFlow es tu <strong>secretaria administrativa virtual</strong> diseñada para micro-pymes. 
                Automatiza la gestión de facturas, conciliación bancaria y tareas administrativas 
                mediante inteligencia artificial.
              </p>
              
              <div class="feature-cards">
                <div class="feature-card">
                  <i class="pi pi-file-pdf"></i>
                  <h4>Extracción Automática</h4>
                  <p>Sube facturas PDF y la IA extrae automáticamente todos los datos</p>
                </div>
                <div class="feature-card">
                  <i class="pi pi-chart-bar"></i>
                  <h4>Estadísticas en Tiempo Real</h4>
                  <p>Visualiza resúmenes de facturación, IVA y totales al instante</p>
                </div>
                <div class="feature-card">
                  <i class="pi pi-palette"></i>
                  <h4>Personalización</h4>
                  <p>Elige entre múltiples temas de colores para adaptar la interfaz</p>
                </div>
                <div class="feature-card">
                  <i class="pi pi-comments"></i>
                  <h4>Chat con IA</h4>
                  <p>Pregunta sobre tus facturas en lenguaje natural</p>
                </div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Dashboard -->
          <p-tabPanel header="📊 Dashboard">
            <div class="help-section">
              <h2><i class="pi pi-home"></i> Panel Principal</h2>
              <p>
                El Dashboard te ofrece una vista general del estado operativo y financiero de tu negocio.
              </p>
              
              <div class="screenshot-container">
                <img src="assets/help/help-dashboard.png" alt="Dashboard de FresiaFlow" class="help-screenshot" />
              </div>

              <h3>Elementos del Dashboard:</h3>
              <ul>
                <li><strong>Tareas Pendientes:</strong> Número de tareas que requieren tu atención</li>
                <li><strong>Alta Prioridad:</strong> Contador de elementos urgentes</li>
                <li><strong>Alertas Críticas:</strong> Avisos importantes del sistema</li>
                <li><strong>Saldo Total:</strong> Resumen de tus cuentas bancarias conectadas</li>
                <li><strong>Tareas Pendientes:</strong> Lista de facturas que requieren verificación manual</li>
                <li><strong>Resumen Bancario:</strong> Estado de los bancos conectados</li>
                <li><strong>Alertas:</strong> Notificaciones del sistema</li>
              </ul>
            </div>
          </p-tabPanel>

          <!-- Tab: Facturas -->
          <p-tabPanel header="📄 Facturas">
            <div class="help-section">
              <h2><i class="pi pi-file"></i> Gestión de Facturas</h2>
              <p>
                La pantalla de Facturas es el corazón de FresiaFlow. Aquí gestionas todas tus facturas recibidas.
              </p>

              <div class="screenshot-container">
                <img src="assets/help/help-invoices.png" alt="Pantalla de Facturas" class="help-screenshot" />
              </div>

              <h3>📈 Panel de Estadísticas</h3>
              <p>En la parte superior verás 7 tarjetas con información resumida:</p>
              <ul>
                <li><strong>Total Facturas:</strong> Número total de facturas en el sistema</li>
                <li><strong>Pago Banco:</strong> Facturas pagadas mediante transferencia</li>
                <li><strong>Pago Efectivo:</strong> Facturas pagadas en efectivo</li>
                <li><strong>Baja Confianza:</strong> Facturas que necesitan revisión manual</li>
                <li><strong>Total Facturado:</strong> Suma de todos los importes</li>
                <li><strong>Total IVA:</strong> IVA acumulado de todas las facturas</li>
                <li><strong>Base Imponible:</strong> Suma de todas las bases imponibles</li>
              </ul>

              <h3>🔍 Filtros Contables</h3>
              <p>Filtra facturas por:</p>
              <ul>
                <li><strong>Año Fiscal:</strong> Selecciona el año a consultar</li>
                <li><strong>Trimestre:</strong> Q1, Q2, Q3 o Q4</li>
                <li><strong>Proveedor:</strong> Busca por nombre de proveedor</li>
                <li><strong>Tipo de Pago:</strong> Banco o Efectivo</li>
              </ul>

              <h3>📤 Subir Facturas</h3>
              <p>Para subir una nueva factura:</p>
              <ol>
                <li>Haz clic en el área de carga o arrastra archivos</li>
                <li>Formatos soportados: PDF, JPG, PNG, GIF, WEBP</li>
                <li>Puedes subir múltiples archivos a la vez</li>
                <li>La IA extraerá automáticamente los datos</li>
              </ol>

              <h3>📋 Tabla de Facturas</h3>
              <p>Funcionalidades de la tabla:</p>
              <ul>
                <li><strong>Ordenación:</strong> Haz clic en las cabeceras para ordenar</li>
                <li><strong>Filtrado por columna:</strong> Usa los campos debajo de cada cabecera</li>
                <li><strong>Paginación:</strong> Navega entre páginas (10 facturas por defecto)</li>
                <li><strong>Detalle de líneas:</strong> Haz clic en el número de líneas para expandir</li>
                <li><strong>Acciones:</strong> Editar o eliminar cada factura</li>
              </ul>

              <h3>💬 Chat IA</h3>
              <p>Pulsa el botón "Chat IA" para hacer preguntas sobre tus facturas:</p>
              <ul>
                <li>"¿Cuánto IVA he pagado en el segundo trimestre?"</li>
                <li>"¿Qué proveedor me factura más?"</li>
                <li>"Facturas pagadas en efectivo este año"</li>
              </ul>
            </div>
          </p-tabPanel>

          <!-- Tab: Configuración -->
          <p-tabPanel header="⚙️ Configuración">
            <div class="help-section">
              <h2><i class="pi pi-cog"></i> Configuración del Sistema</h2>
              
              <div class="screenshot-container">
                <img src="assets/help/help-settings.png" alt="Pantalla de Configuración" class="help-screenshot" />
              </div>

              <h3>🏢 Empresas Propias</h3>
              <p>
                Configura aquí los nombres de tus empresas para que el sistema las reconozca 
                y no las procese como proveedores al subir facturas.
              </p>
              <ul>
                <li>Añade todas las variantes del nombre de tu empresa</li>
                <li>Las facturas con estos nombres como proveedor serán ignoradas</li>
                <li>Útil para filtrar facturas emitidas por tu propia empresa</li>
              </ul>

              <h3>🎨 Selector de Tema</h3>
              <p>
                En la barra lateral encontrarás el selector de tema. Elige entre:
              </p>
              <ul>
                <li><strong>Claro:</strong> Tema por defecto con colores claros</li>
                <li><strong>Oscuro:</strong> Tema oscuro para reducir fatiga visual</li>
                <li><strong>Azul:</strong> Tema profesional con tonos azules</li>
                <li><strong>Verde:</strong> Tema fresco con tonos verdes</li>
                <li><strong>Púrpura:</strong> Tema elegante con tonos púrpura</li>
              </ul>
              <p>El tema se aplica a toda la aplicación: menús, botones, tablas, chat y más.</p>
            </div>
          </p-tabPanel>

          <!-- Tab: Chat Fresia -->
          <p-tabPanel header="🤖 Chat Fresia">
            <div class="help-section">
              <h2><i class="pi pi-comments"></i> Asistente Virtual Fresia</h2>
              <p>
                En la esquina inferior derecha encontrarás el botón flotante del Chat Fresia. 
                Este es tu asistente de IA disponible en cualquier pantalla.
              </p>

              <h3>¿Qué puede hacer Fresia?</h3>
              <ul>
                <li><strong>Consultas sobre facturas:</strong> Pregunta por totales, proveedores, fechas</li>
                <li><strong>Ayuda con tareas:</strong> Orientación sobre cómo usar la aplicación</li>
                <li><strong>Análisis de datos:</strong> Resúmenes y estadísticas bajo demanda</li>
                <li><strong>Soporte técnico:</strong> Respuestas a dudas frecuentes</li>
              </ul>

              <h3>Ejemplos de Preguntas</h3>
              <div class="example-questions">
                <div class="example-q">"¿Cuál es el proveedor con mayor facturación?"</div>
                <div class="example-q">"¿Cuánto IVA he pagado este trimestre?"</div>
                <div class="example-q">"¿Hay facturas pendientes de revisión?"</div>
                <div class="example-q">"Muéstrame las facturas de Amazon"</div>
              </div>

              <h3>Consejos de Uso</h3>
              <ul>
                <li>Sé específico en tus preguntas para obtener mejores respuestas</li>
                <li>El chat recuerda el contexto de la conversación</li>
                <li>Puedes minimizar el chat y retomar la conversación después</li>
              </ul>
            </div>
          </p-tabPanel>

          <!-- Tab: FAQ -->
          <p-tabPanel header="❓ FAQ">
            <div class="help-section">
              <h2><i class="pi pi-question-circle"></i> Preguntas Frecuentes</h2>
              
              <p-accordion>
                <p-accordionTab header="¿Qué formatos de factura acepta el sistema?">
                  <p>
                    FresiaFlow acepta archivos <strong>PDF</strong> e imágenes (<strong>JPG, PNG, GIF, WEBP</strong>). 
                    El sistema extrae automáticamente el texto mediante OCR y la IA estructura los datos.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Cómo funciona la extracción con IA?">
                  <p>
                    Usamos OpenAI GPT-4 para analizar el contenido de las facturas y extraer datos estructurados:
                    número de factura, fechas, importes, IVA, proveedor, NIF/CIF, etc. El sistema calcula un 
                    nivel de confianza y marca para revisión las facturas con confianza menor al 80%.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Qué significa 'Baja Confianza' en una factura?">
                  <p>
                    Indica que la IA no pudo extraer todos los datos con certeza. Esto puede ocurrir por:
                  </p>
                  <ul>
                    <li>Calidad baja del PDF o imagen</li>
                    <li>Formato de factura no estándar</li>
                    <li>Datos ilegibles o borrosos</li>
                    <li>Idiomas no soportados</li>
                  </ul>
                  <p>Revisa y corrige manualmente estos campos antes de guardar.</p>
                </p-accordionTab>

                <p-accordionTab header="¿Es seguro conectar mi banco?">
                  <p>
                    Sí. Usamos estándares Open Banking (PSD2) que requieren tu autorización explícita. 
                    Las credenciales bancarias nunca se almacenan en nuestro sistema. Solo accedemos 
                    a los movimientos que autorices y mediante APIs seguras del banco.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Puedo exportar mis datos?">
                  <p>
                    Sí, puedes exportar facturas a <strong>Excel</strong> usando el botón "Exportar Excel" 
                    en la pantalla de Facturas. El archivo incluye todos los datos estructurados para 
                    importar en tu gestoría contable.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Cómo cambio el tema de la aplicación?">
                  <p>
                    En la barra lateral (sidebar), encontrarás el selector "Tema" con un desplegable. 
                    Selecciona el tema que prefieras y se aplicará instantáneamente a toda la aplicación.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Qué hago si la extracción es incorrecta?">
                  <p>
                    Puedes editar manualmente cualquier campo usando el botón de editar (lápiz) en la 
                    tabla de facturas. El sistema mejora con el tiempo basándose en las correcciones.
                  </p>
                </p-accordionTab>
              </p-accordion>
            </div>
          </p-tabPanel>

          <!-- Tab: Info Técnica -->
          <p-tabPanel header="🔧 Info Técnica">
            <div class="help-section">
              <h2><i class="pi pi-server"></i> Información Técnica</h2>
              
              <div class="tech-grid">
                <div class="tech-item">
                  <span class="tech-label">Versión</span>
                  <span class="tech-value">1.1.0</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Arquitectura</span>
                  <span class="tech-value">Hexagonal (Ports & Adapters)</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Backend</span>
                  <span class="tech-value">ASP.NET Core 8.0 (C#)</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Frontend</span>
                  <span class="tech-value">Angular 17 + PrimeNG</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Base de datos</span>
                  <span class="tech-value">PostgreSQL</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">IA</span>
                  <span class="tech-value">OpenAI GPT-4</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Bancos</span>
                  <span class="tech-value">Open Banking AIS (PSD2)</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Última actualización</span>
                  <span class="tech-value">Diciembre 2025</span>
                </div>
              </div>

              <h3>Novedades de esta versión</h3>
              <ul>
                <li>✨ Sistema de temas personalizables (Claro, Oscuro, Azul, Verde, Púrpura)</li>
                <li>📊 Panel de estadísticas mejorado con 7 indicadores</li>
                <li>🔍 Filtrado avanzado por columnas en la tabla de facturas</li>
                <li>📄 Paginación con 10 facturas por defecto</li>
                <li>🖼️ Soporte para imágenes de facturas (JPG, PNG, GIF, WEBP)</li>
                <li>💬 Chat Fresia disponible en todas las pantallas</li>
                <li>🎨 Interfaz responsive mejorada</li>
              </ul>
            </div>
          </p-tabPanel>
        </p-tabView>
      </div>

      <ng-template pTemplate="footer">
        <div class="footer-content">
          <span class="footer-text">
            <i class="pi pi-heart"></i> Desarrollado por Fresia Software Solutions
          </span>
          <p-button 
            label="Cerrar" 
            icon="pi pi-times" 
            (onClick)="onClose()"
            styleClass="p-button-primary">
          </p-button>
        </div>
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .help-content {
      max-height: 70vh;
      overflow-y: auto;
      padding: 0;
    }

    .help-section {
      padding: 1.5rem;
    }

    .help-section h2 {
      color: var(--primary-color, #dc2626);
      font-size: 1.4rem;
      margin-bottom: 1rem;
      margin-top: 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .help-section h2 i {
      font-size: 1.2rem;
    }

    .help-section h3 {
      color: var(--text-color, #1f2937);
      font-size: 1.1rem;
      margin: 1.5rem 0 0.75rem 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .help-section p {
      line-height: 1.7;
      color: var(--text-color, #4b5563);
      margin-bottom: 1rem;
    }

    .help-section ul, .help-section ol {
      padding-left: 1.5rem;
      line-height: 1.8;
      color: var(--text-color, #4b5563);
      margin: 0.5rem 0 1rem 0;
    }

    .help-section ul li, .help-section ol li {
      margin-bottom: 0.5rem;
    }

    .help-section ul li strong, .help-section ol li strong {
      color: var(--text-color, #1f2937);
    }

    /* Feature Cards */
    .feature-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin: 1.5rem 0;
    }

    .feature-card {
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-radius: 12px;
      padding: 1.25rem;
      text-align: center;
      transition: all 0.3s ease;
    }

    .feature-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
      border-color: var(--primary-color, #dc2626);
    }

    .feature-card i {
      font-size: 2rem;
      color: var(--primary-color, #dc2626);
      margin-bottom: 0.75rem;
    }

    .feature-card h4 {
      color: var(--text-color, #1f2937);
      font-size: 1rem;
      margin: 0 0 0.5rem 0;
    }

    .feature-card p {
      font-size: 0.85rem;
      color: var(--text-color, #6b7280);
      margin: 0;
    }

    /* Screenshots */
    .screenshot-container {
      margin: 1.5rem 0;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .help-screenshot {
      width: 100%;
      height: auto;
      display: block;
    }

    /* Example Questions */
    .example-questions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin: 1rem 0;
    }

    .example-q {
      background: var(--secondary-color, #fee2e2);
      color: var(--text-color, #1f2937);
      padding: 0.5rem 1rem;
      border-radius: 20px;
      font-size: 0.875rem;
      font-style: italic;
    }

    /* Tech Grid */
    .tech-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1.5rem 0;
    }

    .tech-item {
      display: flex;
      justify-content: space-between;
      padding: 0.75rem 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .tech-label {
      color: var(--text-color, #6b7280);
      font-weight: 500;
    }

    .tech-value {
      color: var(--text-color, #1f2937);
      font-weight: 600;
    }

    /* Dialog Header */
    .dialog-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem 1.5rem;
      width: 100%;
    }

    .dialog-header i {
      font-size: 1.75rem;
      color: white;
    }

    .dialog-header span {
      font-size: 1.4rem;
      font-weight: 600;
      color: white;
    }

    .version-badge {
      background: rgba(255, 255, 255, 0.2);
      padding: 0.25rem 0.75rem;
      border-radius: 12px;
      font-size: 0.8rem !important;
      margin-left: auto;
    }

    /* Footer */
    .footer-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0.5rem;
    }

    .footer-text {
      color: var(--text-color, #6b7280);
      font-size: 0.875rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .footer-text i {
      color: var(--primary-color, #dc2626);
    }

    /* PrimeNG Overrides */
    ::ng-deep .p-dialog .p-dialog-header {
      background: linear-gradient(135deg, var(--primary-color, #dc2626) 0%, var(--primary-color-strong, #b91c1c) 100%);
      color: white;
      border-radius: 6px 6px 0 0;
      padding: 0;
      margin: 0;
    }

    ::ng-deep .p-dialog .p-dialog-header .p-dialog-header-icon {
      color: white;
      margin-right: 1rem;
    }

    ::ng-deep .p-dialog .p-dialog-header .p-dialog-header-icon:hover {
      background: rgba(255, 255, 255, 0.1);
    }

    ::ng-deep .p-dialog .p-dialog-content {
      padding: 0;
      background: var(--card-bg, white);
    }

    ::ng-deep .p-dialog .p-dialog-footer {
      background: var(--background-color, #f9fafb);
      border-top: 1px solid var(--secondary-color, #e5e7eb);
    }

    ::ng-deep .p-tabview .p-tabview-nav {
      background: var(--background-color, #f9fafb);
      border-bottom: 1px solid var(--secondary-color, #e5e7eb);
    }

    ::ng-deep .p-tabview .p-tabview-nav li .p-tabview-nav-link {
      background: transparent;
      color: var(--text-color, #6b7280);
      border: none;
      padding: 1rem 1.25rem;
      font-weight: 500;
    }

    ::ng-deep .p-tabview .p-tabview-nav li.p-highlight .p-tabview-nav-link {
      color: var(--primary-color, #dc2626);
      border-bottom: 3px solid var(--primary-color, #dc2626);
    }

    ::ng-deep .p-tabview .p-tabview-nav li:not(.p-highlight):not(.p-disabled):hover .p-tabview-nav-link {
      color: var(--primary-color, #dc2626);
      background: transparent;
    }

    ::ng-deep .p-tabview .p-tabview-panels {
      background: var(--card-bg, white);
      padding: 0;
    }

    ::ng-deep .p-accordion .p-accordion-tab {
      margin-bottom: 0.5rem;
    }

    ::ng-deep .p-accordion .p-accordion-header .p-accordion-header-link {
      background: var(--background-color, #fef2f2);
      border: 1px solid var(--secondary-color, #fee2e2);
      color: var(--text-color, #1f2937);
      font-weight: 600;
      padding: 1rem 1.25rem;
    }

    ::ng-deep .p-accordion .p-accordion-header:not(.p-disabled) .p-accordion-header-link:hover {
      background: var(--secondary-color, #fee2e2);
      border-color: var(--primary-color, #fca5a5);
    }

    ::ng-deep .p-accordion .p-accordion-content {
      background: var(--card-bg, white);
      border: 1px solid var(--secondary-color, #fee2e2);
      border-top: none;
      padding: 1.25rem;
      color: var(--text-color, #4b5563);
    }

    @media (max-width: 768px) {
      .feature-cards {
        grid-template-columns: 1fr;
      }

      .tech-grid {
        grid-template-columns: 1fr;
      }

      .dialog-header span {
        font-size: 1.1rem;
      }

      .version-badge {
        display: none;
      }
    }
  `]
})
export class HelpDialogComponent {
  visible = false;

  show() {
    this.visible = true;
  }

  hide() {
    this.visible = false;
  }

  onClose() {
    this.visible = false;
  }
}
