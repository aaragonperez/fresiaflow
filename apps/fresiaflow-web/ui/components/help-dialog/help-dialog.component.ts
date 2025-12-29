import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { AccordionModule } from 'primeng/accordion';
import { DividerModule } from 'primeng/divider';
import { TagModule } from 'primeng/tag';
import { TabViewModule } from 'primeng/tabview';
import { ImageModule } from 'primeng/image';
import { CardModule } from 'primeng/card';
import { StepsModule } from 'primeng/steps';

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
    ImageModule,
    CardModule,
    StepsModule
  ],
  template: `
    <p-dialog 
      [(visible)]="visible" 
      [modal]="true" 
      [style]="{width: '1200px', maxWidth: '95vw'}"
      [draggable]="false"
      [resizable]="false"
      (onHide)="onClose()">
      
      <ng-template pTemplate="header">
        <div class="dialog-header">
          <i class="pi pi-question-circle"></i>
          <span>Guía de Ayuda de FresiaFlow</span>
          <span class="version-badge">v1.2.0</span>
        </div>
      </ng-template>
      
      <div class="help-content">
        <p-tabView>
          <!-- Tab: Inicio -->
          <p-tabPanel header="🏠 Inicio">
            <div class="help-section">
              <div class="intro-banner">
                <h1><i class="pi pi-info-circle"></i> Bienvenido a FresiaFlow</h1>
                <p class="intro-text">
                  Tu <strong>secretaria administrativa virtual</strong> diseñada para micro-pymes. 
                  Automatiza la gestión de facturas, conciliación bancaria y tareas administrativas 
                  mediante inteligencia artificial.
                </p>
              </div>
              
              <div class="feature-cards">
                <div class="feature-card">
                  <div class="feature-icon">
                    <i class="pi pi-file-pdf"></i>
                  </div>
                  <h4>Extracción Automática</h4>
                  <p>Sube facturas PDF o imágenes y la IA extrae automáticamente todos los datos: proveedor, importes, IVA, fechas y más.</p>
                </div>
                <div class="feature-card">
                  <div class="feature-icon">
                    <i class="pi pi-chart-bar"></i>
                  </div>
                  <h4>Estadísticas en Tiempo Real</h4>
                  <p>Visualiza resúmenes de facturación, IVA, totales y análisis contables al instante en tu Dashboard.</p>
                </div>
                <div class="feature-card">
                  <div class="feature-icon">
                    <i class="pi pi-cloud"></i>
                  </div>
                  <h4>Sincronización OneDrive</h4>
                  <p>Conecta OneDrive y las facturas se procesan automáticamente sin intervención manual.</p>
                </div>
                <div class="feature-card">
                  <div class="feature-icon">
                    <i class="pi pi-comments"></i>
                  </div>
                  <h4>Chat con IA Fresia</h4>
                  <p>Pregunta sobre tus facturas en lenguaje natural. "¿Cuánto IVA pagué este trimestre?"</p>
                </div>
              </div>

              <div class="quick-start">
                <h2><i class="pi pi-rocket"></i> Inicio Rápido</h2>
                <div class="steps-container">
                  <div class="step-item">
                    <div class="step-number">1</div>
                    <div class="step-content">
                      <h4>Sube tu primera factura</h4>
                      <p>Ve a <strong>Facturas</strong> y arrastra un PDF o imagen. La IA extraerá los datos automáticamente.</p>
                    </div>
                  </div>
                  <div class="step-item">
                    <div class="step-number">2</div>
                    <div class="step-content">
                      <h4>Revisa y corrige</h4>
                      <p>Si la confianza es baja, edita los campos manualmente usando el botón de editar (lápiz).</p>
                    </div>
                  </div>
                  <div class="step-item">
                    <div class="step-number">3</div>
                    <div class="step-content">
                      <h4>Configura OneDrive (opcional)</h4>
                      <p>En <strong>Configuración → Sincronización OneDrive</strong> conecta tu cuenta para automatizar la carga.</p>
                    </div>
                  </div>
                  <div class="step-item">
                    <div class="step-number">4</div>
                    <div class="step-content">
                      <h4>Usa el Chat Fresia</h4>
                      <p>Haz clic en el botón flotante del chat (esquina inferior derecha) para hacer preguntas sobre tus datos.</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Dashboard -->
          <p-tabPanel header="📊 Dashboard">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-home"></i> Panel Principal (Dashboard)</h2>
                <p class="section-description">
                  Tu centro de control. Aquí verás un resumen completo del estado de tu negocio en un solo vistazo.
                </p>
              </div>
              
              <div class="screenshot-container">
                <img src="assets/help/help-dashboard.png" alt="Dashboard de FresiaFlow" class="help-screenshot" />
                <div class="screenshot-note">
                  <i class="pi pi-info-circle"></i>
                  <span>Captura de pantalla del Dashboard mostrando todas las tarjetas de resumen y widgets</span>
                </div>
              </div>

              <div class="info-grid">
                <div class="info-card">
                  <div class="info-card-header">
                    <i class="pi pi-check-square"></i>
                    <h3>Tarjetas de Resumen</h3>
                  </div>
                  <ul>
                    <li><strong>Tareas Pendientes:</strong> Total de tareas que requieren tu atención</li>
                    <li><strong>Alta Prioridad:</strong> Tareas urgentes marcadas en rojo</li>
                    <li><strong>Alertas Críticas:</strong> Avisos importantes que necesitan acción inmediata</li>
                    <li><strong>Saldo Total:</strong> Suma de todas tus cuentas bancarias conectadas</li>
                  </ul>
                </div>

                <div class="info-card">
                  <div class="info-card-header">
                    <i class="pi pi-list"></i>
                    <h3>Widgets Principales</h3>
                  </div>
                  <ul>
                    <li><strong>Lista de Tareas:</strong> Facturas pendientes de revisión manual</li>
                    <li><strong>Resumen Bancario:</strong> Estado y saldos de bancos conectados</li>
                    <li><strong>Alertas del Sistema:</strong> Notificaciones y recordatorios</li>
                    <li><strong>Estado de Sincronización:</strong> Última sincronización con OneDrive</li>
                  </ul>
                </div>
              </div>

              <div class="tips-box">
                <h3><i class="pi pi-lightbulb"></i> Consejos de Uso</h3>
                <ul>
                  <li>✅ Revisa el Dashboard cada mañana para ver tareas pendientes</li>
                  <li>⚠️ Las alertas críticas aparecen en rojo y requieren atención inmediata</li>
                  <li>💰 El saldo bancario se actualiza automáticamente al conectar tus cuentas</li>
                  <li>🔴 Las tareas de alta prioridad están destacadas para que no se te pasen</li>
                </ul>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Facturas -->
          <p-tabPanel header="📄 Facturas">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-file"></i> Gestión de Facturas</h2>
                <p class="section-description">
                  El corazón de FresiaFlow. Aquí gestionas todas tus facturas recibidas con extracción automática de datos mediante IA.
                </p>
              </div>

              <div class="screenshot-container">
                <img src="assets/help/help-invoices.png" alt="Pantalla de Facturas" class="help-screenshot" />
                <div class="screenshot-note">
                  <i class="pi pi-info-circle"></i>
                  <span>Vista completa de la pantalla de Facturas con estadísticas, filtros y tabla</span>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-chart-line"></i> Panel de Estadísticas</h3>
                <p>En la parte superior encontrarás 7 tarjetas con métricas clave:</p>
                <div class="stats-grid">
                  <div class="stat-item">
                    <i class="pi pi-file"></i>
                    <strong>Total Facturas</strong>
                    <span>Número total de facturas en el sistema</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-credit-card"></i>
                    <strong>Pago Banco</strong>
                    <span>Facturas pagadas mediante transferencia</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-money-bill"></i>
                    <strong>Pago Efectivo</strong>
                    <span>Facturas pagadas en efectivo</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-exclamation-triangle"></i>
                    <strong>Baja Confianza</strong>
                    <span>Facturas que necesitan revisión manual</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-euro"></i>
                    <strong>Total Facturado</strong>
                    <span>Suma de todos los importes</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-percentage"></i>
                    <strong>Total IVA</strong>
                    <span>IVA acumulado de todas las facturas</span>
                  </div>
                  <div class="stat-item">
                    <i class="pi pi-calculator"></i>
                    <strong>Base Imponible</strong>
                    <span>Suma de todas las bases imponibles</span>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-filter"></i> Filtros Contables</h3>
                <p>Filtra tus facturas para análisis contables específicos:</p>
                <div class="filter-list">
                  <div class="filter-item">
                    <i class="pi pi-calendar"></i>
                    <div>
                      <strong>Año Fiscal</strong>
                      <p>Selecciona el año a consultar (desde 2014 hasta el actual)</p>
                    </div>
                  </div>
                  <div class="filter-item">
                    <i class="pi pi-calendar-times"></i>
                    <div>
                      <strong>Trimestre</strong>
                      <p>Filtra por Q1, Q2, Q3 o Q4 del año seleccionado</p>
                    </div>
                  </div>
                  <div class="filter-item">
                    <i class="pi pi-building"></i>
                    <div>
                      <strong>Proveedor</strong>
                      <p>Busca por nombre de proveedor (búsqueda parcial)</p>
                    </div>
                  </div>
                  <div class="filter-item">
                    <i class="pi pi-wallet"></i>
                    <div>
                      <strong>Tipo de Pago</strong>
                      <p>Filtra por Banco o Efectivo</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-upload"></i> Subir Facturas</h3>
                <p>Sigue estos pasos para subir facturas:</p>
                <div class="steps-list">
                  <div class="step-box">
                    <div class="step-number">1</div>
                    <div class="step-text">
                      <strong>Arrastra y suelta</strong> o haz clic en el área de carga
                    </div>
                  </div>
                  <div class="step-box">
                    <div class="step-number">2</div>
                    <div class="step-text">
                      <strong>Formatos soportados:</strong> PDF, JPG, PNG, GIF, WEBP
                    </div>
                  </div>
                  <div class="step-box">
                    <div class="step-number">3</div>
                    <div class="step-text">
                      Puedes subir <strong>múltiples archivos</strong> a la vez
                    </div>
                  </div>
                  <div class="step-box">
                    <div class="step-number">4</div>
                    <div class="step-text">
                      La <strong>IA extraerá automáticamente</strong> todos los datos
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-table"></i> Tabla de Facturas</h3>
                <p>Funcionalidades avanzadas de la tabla:</p>
                <ul class="feature-list">
                  <li><i class="pi pi-sort"></i> <strong>Ordenación:</strong> Haz clic en las cabeceras de columna para ordenar</li>
                  <li><i class="pi pi-filter"></i> <strong>Filtrado por columna:</strong> Usa los campos de filtro debajo de cada cabecera</li>
                  <li><i class="pi pi-search"></i> <strong>Búsqueda global:</strong> Busca en todos los campos desde el filtro superior</li>
                  <li><i class="pi pi-list"></i> <strong>Paginación:</strong> Navega entre páginas (10, 25 o 50 facturas por página)</li>
                  <li><i class="pi pi-eye"></i> <strong>Detalle de líneas:</strong> Haz clic en el número de líneas para expandir y ver detalles</li>
                  <li><i class="pi pi-pencil"></i> <strong>Editar:</strong> Usa el botón de lápiz para corregir datos extraídos</li>
                  <li><i class="pi pi-trash"></i> <strong>Eliminar:</strong> Elimina facturas que no necesites</li>
                </ul>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-comments"></i> Chat IA</h3>
                <p>Usa el botón "Chat IA" para hacer preguntas en lenguaje natural:</p>
                <div class="example-boxes">
                  <div class="example-box">"¿Cuánto IVA he pagado en el segundo trimestre?"</div>
                  <div class="example-box">"¿Qué proveedor me factura más?"</div>
                  <div class="example-box">"Muéstrame las facturas pagadas en efectivo este año"</div>
                  <div class="example-box">"Facturas de Amazon del último mes"</div>
                </div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Configuración -->
          <p-tabPanel header="⚙️ Configuración">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-cog"></i> Configuración del Sistema</h2>
                <p class="section-description">
                  Personaliza FresiaFlow según las necesidades de tu negocio. Configura empresas, OneDrive y temas.
                </p>
              </div>
              
              <div class="screenshot-container">
                <img src="assets/help/help-settings.png" alt="Pantalla de Configuración" class="help-screenshot" />
                <div class="screenshot-note">
                  <i class="pi pi-info-circle"></i>
                  <span>Pantalla de Configuración con secciones colapsables para cada opción</span>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-building"></i> Empresas Propias</h3>
                <p>
                  Configura los nombres de tus empresas para que el sistema las reconozca y no las procese como proveedores.
                </p>
                <div class="info-box">
                  <div class="info-box-header">
                    <i class="pi pi-info-circle"></i>
                    <strong>¿Por qué es importante?</strong>
                  </div>
                  <p>Si tu empresa emite facturas a clientes, estas aparecerían como proveedores. Al configurar tus empresas propias, el sistema las ignorará automáticamente.</p>
                </div>
                <div class="steps-list">
                  <div class="step-box">
                    <div class="step-number">1</div>
                    <div class="step-text">
                      Ve a <strong>Configuración → Empresas Propias</strong>
                    </div>
                  </div>
                  <div class="step-box">
                    <div class="step-number">2</div>
                    <div class="step-text">
                      Añade <strong>todas las variantes</strong> del nombre de tu empresa (con y sin S.L., S.A., etc.)
                    </div>
                  </div>
                  <div class="step-box">
                    <div class="step-number">3</div>
                    <div class="step-text">
                      Haz clic en <strong>Guardar</strong>. Las facturas con estos nombres serán ignoradas
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-microsoft"></i> Sincronización OneDrive</h3>
                <p>
                  Conecta OneDrive para automatizar completamente la carga de facturas. 
                  Ve a la pestaña <strong>"☁️ OneDrive"</strong> para ver la guía completa paso a paso.
                </p>
                <div class="quick-info">
                  <div class="quick-info-item">
                    <i class="pi pi-cloud"></i>
                    <div>
                      <strong>Carpeta OneDrive</strong>
                      <p>Especifica la ruta de la carpeta a sincronizar (ej: /Facturas)</p>
                    </div>
                  </div>
                  <div class="quick-info-item">
                    <i class="pi pi-clock"></i>
                    <div>
                      <strong>Sincronización Automática</strong>
                      <p>Configura el intervalo (mínimo 15 minutos)</p>
                    </div>
                  </div>
                  <div class="quick-info-item">
                    <i class="pi pi-refresh"></i>
                    <div>
                      <strong>Sincronización Manual</strong>
                      <p>Fuerza una sincronización inmediata cuando lo necesites</p>
                    </div>
                  </div>
                  <div class="quick-info-item">
                    <i class="pi pi-history"></i>
                    <div>
                      <strong>Historial</strong>
                      <p>Visualiza todos los archivos sincronizados y su estado</p>
                    </div>
                  </div>
                </div>
                <div class="format-badge">
                  <i class="pi pi-file"></i>
                  <span><strong>Formatos soportados:</strong> PDF, JPG, JPEG, PNG, GIF, WEBP</span>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-palette"></i> Selector de Tema</h3>
                <p>
                  Personaliza la apariencia de FresiaFlow. En la barra lateral encontrarás el selector de tema.
                </p>
                <div class="theme-grid">
                  <div class="theme-item">
                    <div class="theme-color" style="background: linear-gradient(135deg, #ffffff 0%, #f3f4f6 100%);"></div>
                    <strong>Claro</strong>
                    <span>Tema por defecto con colores claros</span>
                  </div>
                  <div class="theme-item">
                    <div class="theme-color" style="background: linear-gradient(135deg, #1f2937 0%, #111827 100%);"></div>
                    <strong>Oscuro</strong>
                    <span>Reduce fatiga visual</span>
                  </div>
                  <div class="theme-item">
                    <div class="theme-color" style="background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);"></div>
                    <strong>Azul</strong>
                    <span>Tema profesional</span>
                  </div>
                  <div class="theme-item">
                    <div class="theme-color" style="background: linear-gradient(135deg, #10b981 0%, #059669 100%);"></div>
                    <strong>Verde</strong>
                    <span>Tema fresco</span>
                  </div>
                  <div class="theme-item">
                    <div class="theme-color" style="background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);"></div>
                    <strong>Púrpura</strong>
                    <span>Tema elegante</span>
                  </div>
                </div>
                <p class="note-text">
                  <i class="pi pi-info-circle"></i>
                  El tema se aplica instantáneamente a toda la aplicación: menús, botones, tablas, chat y más.
                </p>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Tareas -->
          <p-tabPanel header="✅ Tareas">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-check-square"></i> Gestión de Tareas</h2>
                <p class="section-description">
                  Organiza y prioriza tu trabajo administrativo. El sistema crea tareas automáticamente cuando detecta elementos que requieren tu atención.
                </p>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-list"></i> Tipos de Tareas</h3>
                <div class="task-types">
                  <div class="task-type-item">
                    <i class="pi pi-file-edit"></i>
                    <div>
                      <strong>Revisión de Facturas</strong>
                      <p>Facturas con baja confianza (&lt;80%) que necesitan validación manual</p>
                    </div>
                  </div>
                  <div class="task-type-item">
                    <i class="pi pi-wallet"></i>
                    <div>
                      <strong>Conciliación Bancaria</strong>
                      <p>Transacciones bancarias que requieren asociación con facturas</p>
                    </div>
                  </div>
                  <div class="task-type-item">
                    <i class="pi pi-calendar-plus"></i>
                    <div>
                      <strong>Tareas Administrativas</strong>
                      <p>Recordatorios y tareas personalizadas que creas manualmente</p>
                    </div>
                  </div>
                  <div class="task-type-item">
                    <i class="pi pi-bell"></i>
                    <div>
                      <strong>Alertas del Sistema</strong>
                      <p>Notificaciones importantes que requieren acción inmediata</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-flag"></i> Niveles de Prioridad</h3>
                <p>Las tareas se clasifican en tres niveles de prioridad con códigos de color:</p>
                <div class="priority-list">
                  <div class="priority-item priority-high">
                    <div class="priority-badge"></div>
                    <div>
                      <strong>Alta Prioridad</strong>
                      <p>Requieren atención inmediata. Aparecen destacadas en rojo.</p>
                    </div>
                  </div>
                  <div class="priority-item priority-medium">
                    <div class="priority-badge"></div>
                    <div>
                      <strong>Prioridad Media</strong>
                      <p>Importantes pero no urgentes. Marcadas en naranja.</p>
                    </div>
                  </div>
                  <div class="priority-item priority-low">
                    <div class="priority-badge"></div>
                    <div>
                      <strong>Prioridad Baja</strong>
                      <p>Pueden esperar. Marcadas en azul.</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-cog"></i> Gestión de Tareas</h3>
                <p>Acciones que puedes realizar con tus tareas:</p>
                <ul class="feature-list">
                  <li><i class="pi pi-check"></i> <strong>Completar:</strong> Marca tareas como completadas haciendo clic en el checkbox</li>
                  <li><i class="pi pi-pencil"></i> <strong>Editar:</strong> Modifica la descripción o cambia la prioridad</li>
                  <li><i class="pi pi-trash"></i> <strong>Eliminar:</strong> Elimina tareas que ya no sean relevantes</li>
                  <li><i class="pi pi-filter"></i> <strong>Filtrar:</strong> Filtra por prioridad o estado para enfocarte en lo importante</li>
                  <li><i class="pi pi-plus"></i> <strong>Crear:</strong> Añade nuevas tareas administrativas manualmente</li>
                </ul>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-bell"></i> Generación Automática</h3>
                <p>El sistema crea tareas automáticamente cuando:</p>
                <div class="auto-tasks">
                  <div class="auto-task-item">
                    <i class="pi pi-exclamation-triangle"></i>
                    <span>Una factura tiene confianza menor al 80%</span>
                  </div>
                  <div class="auto-task-item">
                    <i class="pi pi-link"></i>
                    <span>Hay transacciones bancarias sin conciliar</span>
                  </div>
                  <div class="auto-task-item">
                    <i class="pi pi-times-circle"></i>
                    <span>Se detectan anomalías o errores en el procesamiento</span>
                  </div>
                  <div class="auto-task-item">
                    <i class="pi pi-clock"></i>
                    <span>Hay recordatorios programados que se activan</span>
                  </div>
                </div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Bancos -->
          <p-tabPanel header="💰 Bancos">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-wallet"></i> Conexión Bancaria</h2>
                <p class="section-description">
                  Conecta tus cuentas bancarias para automatizar la conciliación de pagos y tener una visión completa de tu situación financiera.
                </p>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-shield"></i> Seguridad y Privacidad</h3>
                <p>
                  La conexión bancaria utiliza <strong>Open Banking (PSD2)</strong>, el estándar europeo de seguridad bancaria.
                </p>
                <div class="security-features">
                  <div class="security-item">
                    <i class="pi pi-lock"></i>
                    <div>
                      <strong>Sin Almacenamiento de Credenciales</strong>
                      <p>Tus credenciales bancarias nunca se almacenan en nuestro sistema</p>
                    </div>
                  </div>
                  <div class="security-item">
                    <i class="pi pi-link"></i>
                    <div>
                      <strong>Conexión Directa</strong>
                      <p>Conexión segura y directa con tu banco mediante APIs oficiales</p>
                    </div>
                  </div>
                  <div class="security-item">
                    <i class="pi pi-eye"></i>
                    <div>
                      <strong>Solo Lectura</strong>
                      <p>Solo accedemos a transacciones. No podemos realizar pagos ni transferencias</p>
                    </div>
                  </div>
                  <div class="security-item">
                    <i class="pi pi-check-circle"></i>
                    <div>
                      <strong>Autorización Explícita</strong>
                      <p>Requiere tu autorización explícita para cada conexión</p>
                    </div>
                  </div>
                  <div class="security-item">
                    <i class="pi pi-times-circle"></i>
                    <div>
                      <strong>Revocable en Cualquier Momento</strong>
                      <p>Puedes desconectar tu banco cuando quieras desde la configuración</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-briefcase"></i> Funcionalidades</h3>
                <div class="bank-features">
                  <div class="bank-feature-item">
                    <i class="pi pi-download"></i>
                    <div>
                      <strong>Importación Automática</strong>
                      <p>Descarga automática de transacciones desde tus cuentas bancarias</p>
                    </div>
                  </div>
                  <div class="bank-feature-item">
                    <i class="pi pi-sync"></i>
                    <div>
                      <strong>Conciliación Automática</strong>
                      <p>Asocia automáticamente pagos bancarios con facturas</p>
                    </div>
                  </div>
                  <div class="bank-feature-item">
                    <i class="pi pi-credit-card"></i>
                    <div>
                      <strong>Múltiples Cuentas</strong>
                      <p>Conecta todas tus cuentas bancarias en un solo lugar</p>
                    </div>
                  </div>
                  <div class="bank-feature-item">
                    <i class="pi pi-chart-line"></i>
                    <div>
                      <strong>Saldo en Tiempo Real</strong>
                      <p>Visualiza el saldo actualizado de todas tus cuentas</p>
                    </div>
                  </div>
                  <div class="bank-feature-item">
                    <i class="pi pi-history"></i>
                    <div>
                      <strong>Historial Completo</strong>
                      <p>Accede a todos tus movimientos bancarios históricos</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-link"></i> Conciliación Inteligente</h3>
                <p>
                  El sistema intenta automáticamente asociar transacciones bancarias con facturas usando múltiples criterios:
                </p>
                <div class="reconciliation-methods">
                  <div class="method-item">
                    <i class="pi pi-euro"></i>
                    <strong>Por Importe Exacto</strong>
                    <p>Coincidencia cuando el importe de la transacción coincide exactamente con el de la factura</p>
                  </div>
                  <div class="method-item">
                    <i class="pi pi-calendar"></i>
                    <strong>Por Fecha Cercana</strong>
                    <p>Asocia transacciones con facturas basándose en fechas próximas</p>
                  </div>
                  <div class="method-item">
                    <i class="pi pi-building"></i>
                    <strong>Por Concepto/Proveedor</strong>
                    <p>Matching inteligente basado en el concepto de la transacción y el proveedor de la factura</p>
                  </div>
                  <div class="method-item">
                    <i class="pi pi-brain"></i>
                    <strong>Sugerencias con IA</strong>
                    <p>La IA sugiere asociaciones probables cuando hay múltiples coincidencias</p>
                  </div>
                </div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: OneDrive -->
          <p-tabPanel header="☁️ OneDrive">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-microsoft"></i> Sincronización con OneDrive</h2>
                <p class="section-description">
                  Automatiza completamente la carga de facturas. Simplemente guarda tus facturas en OneDrive y FresiaFlow las procesará automáticamente.
                </p>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-check-circle"></i> Requisitos Previos</h3>
                <p>Para configurar la sincronización necesitas:</p>
                <div class="requirement-list">
                  <div class="requirement-item">
                    <i class="pi pi-microsoft"></i>
                    <span>Una cuenta de Microsoft 365 o OneDrive</span>
                  </div>
                  <div class="requirement-item">
                    <i class="pi pi-cloud"></i>
                    <span>Acceso al portal de Azure (portal.azure.com)</span>
                  </div>
                  <div class="requirement-item">
                    <i class="pi pi-shield"></i>
                    <span>Permisos de administrador en Azure AD (recomendado)</span>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-cog"></i> Configuración Paso a Paso</h3>
                
                <div class="setup-step">
                  <div class="setup-step-header">
                    <div class="step-number">1</div>
                    <h4>Crear App Registration en Azure</h4>
                  </div>
                  <div class="setup-step-content">
                    <div class="steps-list">
                      <div class="step-box">
                        <div class="step-number">1</div>
                        <div class="step-text">Accede al <strong>Portal de Azure</strong> (portal.azure.com)</div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">2</div>
                        <div class="step-text">Ve a <strong>Azure Active Directory</strong> → <strong>App registrations</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">3</div>
                        <div class="step-text">Haz clic en <strong>New registration</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">4</div>
                        <div class="step-text">
                          Configura:
                          <ul style="margin-top: 0.5rem; padding-left: 1.5rem;">
                            <li><strong>Name:</strong> FresiaFlow OneDrive Sync</li>
                            <li><strong>Supported account types:</strong> Single tenant</li>
                            <li><strong>Redirect URI:</strong> Deja en blanco</li>
                          </ul>
                        </div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">5</div>
                        <div class="step-text">Haz clic en <strong>Register</strong></div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="setup-step">
                  <div class="setup-step-header">
                    <div class="step-number">2</div>
                    <h4>Obtener las Credenciales</h4>
                  </div>
                  <div class="setup-step-content">
                    <div class="steps-list">
                      <div class="step-box">
                        <div class="step-number">1</div>
                        <div class="step-text">Copia el <strong>Application (client) ID</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">2</div>
                        <div class="step-text">Copia el <strong>Directory (tenant) ID</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">3</div>
                        <div class="step-text">Ve a <strong>Certificates & secrets</strong> → <strong>New client secret</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">4</div>
                        <div class="step-text">Añade descripción y expiración (recomendado: 24 meses)</div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">5</div>
                        <div class="step-text">
                          <strong>⚠️ IMPORTANTE:</strong> Copia el <strong>Value</strong> del secret inmediatamente. 
                          <span style="color: var(--primary-color);">¡Solo se muestra una vez!</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="setup-step">
                  <div class="setup-step-header">
                    <div class="step-number">3</div>
                    <h4>Configurar Permisos API</h4>
                  </div>
                  <div class="setup-step-content">
                    <div class="steps-list">
                      <div class="step-box">
                        <div class="step-number">1</div>
                        <div class="step-text">Ve a <strong>API permissions</strong> → <strong>Add a permission</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">2</div>
                        <div class="step-text">Selecciona <strong>Microsoft Graph</strong> → <strong>Application permissions</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">3</div>
                        <div class="step-text">Añade el permiso <strong>Files.Read.All</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">4</div>
                        <div class="step-text">Haz clic en <strong>Grant admin consent</strong> (requiere permisos de administrador)</div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="setup-step">
                  <div class="setup-step-header">
                    <div class="step-number">4</div>
                    <h4>Configurar en FresiaFlow</h4>
                  </div>
                  <div class="setup-step-content">
                    <div class="steps-list">
                      <div class="step-box">
                        <div class="step-number">1</div>
                        <div class="step-text">Ve a <strong>Configuración</strong> → <strong>Sincronización OneDrive</strong></div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">2</div>
                        <div class="step-text">
                          Completa los campos:
                          <ul style="margin-top: 0.5rem; padding-left: 1.5rem;">
                            <li><strong>Tenant ID:</strong> El Directory (tenant) ID de Azure</li>
                            <li><strong>Client ID:</strong> El Application (client) ID de Azure</li>
                            <li><strong>Client Secret:</strong> El secret que copiaste</li>
                            <li><strong>Ruta de Carpeta:</strong> Ej: /Facturas</li>
                            <li><strong>Drive ID:</strong> (Opcional) Solo para SharePoint/Teams</li>
                          </ul>
                        </div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">3</div>
                        <div class="step-text">Haz clic en <strong>Validar Conexión</strong> para probar</div>
                      </div>
                      <div class="step-box">
                        <div class="step-number">4</div>
                        <div class="step-text">Si la validación es exitosa, haz clic en <strong>Guardar Configuración</strong></div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <h3>⚙️ Opciones de Sincronización</h3>
              
              <h4>Sincronización Automática</h4>
              <ul>
                <li>Activa el switch <strong>Sincronización Automática Habilitada</strong></li>
                <li>Configura el <strong>Intervalo de Sincronización</strong> (mínimo 15 minutos)</li>
                <li>El sistema sincronizará automáticamente en el intervalo configurado</li>
                <li>Solo se procesan archivos nuevos o modificados</li>
              </ul>

              <h4>Sincronización Manual</h4>
              <ul>
                <li>Haz clic en <strong>Sincronizar Ahora</strong> para forzar una sincronización inmediata</li>
                <li>Opción <strong>Forzar Reprocesamiento:</strong> Vuelve a procesar archivos ya sincronizados</li>
                <li>Puedes cancelar una sincronización en curso con el botón <strong>Cancelar</strong></li>
                <li>El progreso se muestra en tiempo real con barra de progreso</li>
              </ul>

              <h3>📊 Historial de Sincronización</h3>
              <p>La tabla de historial muestra todos los archivos sincronizados con:</p>
              <ul>
                <li><strong>Nombre del archivo:</strong> Nombre original en OneDrive</li>
                <li><strong>Estado:</strong> Completado, Procesando, Pendiente, Fallido, Omitido</li>
                <li><strong>Tamaño:</strong> Tamaño del archivo</li>
                <li><strong>Fecha de sincronización:</strong> Cuándo se sincronizó</li>
                <li><strong>Acciones:</strong> Ver el archivo original</li>
              </ul>

              <h3>💡 Consejos y Buenas Prácticas</h3>
              <ul>
                <li>Organiza tus facturas en una carpeta dedicada de OneDrive</li>
                <li>Usa nombres descriptivos para tus archivos (ej: "Factura_Amazon_2024-01.pdf")</li>
                <li>El sistema detecta automáticamente duplicados por hash del archivo</li>
                <li>Los archivos ya procesados se omiten en sincronizaciones posteriores</li>
                <li>Puedes usar subcarpetas; el sistema las explorará recursivamente</li>
                <li>Configura un intervalo de sincronización acorde a tu volumen de facturas</li>
              </ul>

              <h3>⚠️ Solución de Problemas</h3>
              <div class="example-questions">
                <div class="example-q"><strong>Error de autenticación:</strong> Verifica que el Client Secret no haya expirado</div>
                <div class="example-q"><strong>Carpeta no encontrada:</strong> Asegúrate de que la ruta sea correcta (ej: /Facturas)</div>
                <div class="example-q"><strong>Sin permisos:</strong> Verifica que se haya dado consentimiento de administrador</div>
                <div class="example-q"><strong>Archivos no se procesan:</strong> Verifica que sean formatos soportados (PDF, imágenes)</div>
              </div>
            </div>
          </p-tabPanel>

          <!-- Tab: Chat Fresia -->
          <p-tabPanel header="🤖 Chat Fresia">
            <div class="help-section">
              <div class="section-header-large">
                <h2><i class="pi pi-comments"></i> Asistente Virtual Fresia</h2>
                <p class="section-description">
                  Tu asistente de IA disponible en cualquier pantalla. Haz preguntas en lenguaje natural sobre tus facturas y datos.
                </p>
              </div>

              <div class="info-box">
                <div class="info-box-header">
                  <i class="pi pi-info-circle"></i>
                  <strong>¿Dónde está el Chat Fresia?</strong>
                </div>
                <p>
                  En la <strong>esquina inferior derecha</strong> de cualquier pantalla encontrarás el botón flotante del Chat Fresia. 
                  Haz clic para abrir el chat y empezar a conversar.
                </p>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-star"></i> ¿Qué puede hacer Fresia?</h3>
                <div class="chat-capabilities">
                  <div class="capability-item">
                    <i class="pi pi-file"></i>
                    <div>
                      <strong>Consultas sobre Facturas</strong>
                      <p>Pregunta por totales, proveedores, fechas, IVA, trimestres y más</p>
                    </div>
                  </div>
                  <div class="capability-item">
                    <i class="pi pi-question-circle"></i>
                    <div>
                      <strong>Ayuda con la Aplicación</strong>
                      <p>Orientación sobre cómo usar funcionalidades y resolver dudas</p>
                    </div>
                  </div>
                  <div class="capability-item">
                    <i class="pi pi-chart-bar"></i>
                    <div>
                      <strong>Análisis de Datos</strong>
                      <p>Resúmenes y estadísticas bajo demanda sobre tus facturas</p>
                    </div>
                  </div>
                  <div class="capability-item">
                    <i class="pi pi-wrench"></i>
                    <div>
                      <strong>Soporte Técnico</strong>
                      <p>Respuestas a dudas frecuentes y solución de problemas</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="feature-section">
                <h3><i class="pi pi-lightbulb"></i> Ejemplos de Preguntas</h3>
                <p>Prueba estas preguntas para ver qué puede hacer Fresia:</p>
                <div class="example-boxes">
                  <div class="example-box">"¿Cuál es el proveedor con mayor facturación?"</div>
                  <div class="example-box">"¿Cuánto IVA he pagado este trimestre?"</div>
                  <div class="example-box">"¿Hay facturas pendientes de revisión?"</div>
                  <div class="example-box">"Muéstrame las facturas de Amazon"</div>
                  <div class="example-box">"¿Cuántas facturas tengo este mes?"</div>
                  <div class="example-box">"Facturas pagadas en efectivo en 2024"</div>
                </div>
              </div>

              <div class="tips-box">
                <h3><i class="pi pi-info-circle"></i> Consejos de Uso</h3>
                <ul>
                  <li>✅ <strong>Sé específico:</strong> Cuanto más detallada sea tu pregunta, mejor será la respuesta</li>
                  <li>💬 <strong>Contexto persistente:</strong> El chat recuerda la conversación, puedes hacer preguntas de seguimiento</li>
                  <li>📱 <strong>Minimizar y retomar:</strong> Puedes minimizar el chat y retomar la conversación después</li>
                  <li>🔄 <strong>Acciones automáticas:</strong> Fresia puede aplicar filtros, abrir facturas y navegar por la aplicación</li>
                </ul>
              </div>
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

                <p-accordionTab header="¿Cómo funciona la sincronización con OneDrive?">
                  <p>
                    La sincronización con OneDrive conecta tu cuenta de Microsoft 365 con FresiaFlow. 
                    Una vez configurada, el sistema revisa automáticamente una carpeta específica de OneDrive 
                    en intervalos regulares (mínimo 15 minutos) y procesa todas las facturas nuevas que encuentre.
                  </p>
                  <p>
                    Los archivos ya procesados se detectan automáticamente mediante hash, evitando duplicados. 
                    Puedes forzar el reprocesamiento si es necesario.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Es seguro conectar mi OneDrive?">
                  <p>
                    Sí. La conexión se realiza mediante Azure Active Directory con permisos específicos 
                    de solo lectura. Las credenciales se almacenan de forma segura en la base de datos 
                    y solo se usan para acceder a la carpeta específica que configures.
                  </p>
                  <p>
                    Usamos Microsoft Graph API con autenticación OAuth 2.0, el estándar de seguridad 
                    recomendado por Microsoft.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Qué pasa si un archivo falla al procesarse desde OneDrive?">
                  <p>
                    Si un archivo falla durante el procesamiento, se marca como "Fallido" en el historial 
                    de sincronización. El error se registra y puedes ver los detalles en el historial.
                  </p>
                  <p>
                    Puedes intentar reprocesar el archivo usando la opción "Forzar Reprocesamiento" en 
                    la sincronización manual. Si el problema persiste, verifica que el archivo sea un 
                    formato válido y que contenga texto legible.
                  </p>
                </p-accordionTab>

                <p-accordionTab header="¿Puedo usar OneDrive de empresa o SharePoint?">
                  <p>
                    Sí. FresiaFlow soporta OneDrive personal, OneDrive for Business y SharePoint. 
                    Para SharePoint o Teams, necesitarás proporcionar el <strong>Drive ID</strong> 
                    además de la ruta de la carpeta.
                  </p>
                  <p>
                    El Drive ID lo puedes obtener mediante Microsoft Graph Explorer o consultando 
                    con tu administrador de Microsoft 365.
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
                  <span class="tech-value">1.2.0</span>
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
                  <span class="tech-value">OpenAI GPT-4o-mini</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Bancos</span>
                  <span class="tech-value">Open Banking AIS (PSD2)</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Sincronización</span>
                  <span class="tech-value">Microsoft Graph API + SignalR</span>
                </div>
                <div class="tech-item">
                  <span class="tech-label">Última actualización</span>
                  <span class="tech-value">Diciembre 2025</span>
                </div>
              </div>

              <h3>Novedades de esta versión (v1.2.0)</h3>
              <ul>
                <li>☁️ <strong>Sincronización con OneDrive:</strong> Automatiza la carga de facturas desde OneDrive/SharePoint</li>
                <li>📡 <strong>Progreso en tiempo real:</strong> Visualiza el progreso de sincronización con SignalR</li>
                <li>📊 <strong>Historial de sincronización:</strong> Tabla completa con todos los archivos procesados</li>
                <li>🔄 <strong>Sincronización automática:</strong> Configura intervalos de sincronización personalizados</li>
                <li>🎯 <strong>Detección de duplicados:</strong> Sistema de hash para evitar reprocesar archivos</li>
                <li>🚀 <strong>Validación de conexión:</strong> Prueba tu configuración antes de guardar</li>
                <li>⚙️ <strong>Sistema de agentes IA:</strong> Router inteligente para desarrollo (ARQ, DOM, INT, IA, REV, COG, PO, TEST, DOC, AYU)</li>
              </ul>

              <h3>Versiones anteriores</h3>
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

    /* New Styles for Enhanced Help */
    .intro-banner {
      background: linear-gradient(135deg, var(--primary-color, #dc2626) 0%, var(--primary-color-strong, #b91c1c) 100%);
      color: white;
      padding: 2rem;
      border-radius: 12px;
      margin-bottom: 2rem;
      text-align: center;
    }

    .intro-banner h1 {
      color: white;
      font-size: 1.8rem;
      margin: 0 0 1rem 0;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
    }

    .intro-text {
      font-size: 1.1rem;
      line-height: 1.8;
      margin: 0;
      opacity: 0.95;
    }

    .section-header-large {
      margin-bottom: 2rem;
    }

    .section-header-large h2 {
      font-size: 1.6rem;
      margin-bottom: 0.5rem;
    }

    .section-description {
      font-size: 1.05rem;
      color: var(--text-color, #6b7280);
      margin: 0;
    }

    .screenshot-note {
      background: var(--background-color, #f9fafb);
      padding: 0.75rem 1rem;
      border-top: 1px solid var(--secondary-color, #e5e7eb);
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: var(--text-color, #6b7280);
    }

    .screenshot-note i {
      color: var(--primary-color, #dc2626);
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1.5rem;
      margin: 1.5rem 0;
    }

    .info-card {
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-radius: 12px;
      padding: 1.5rem;
    }

    .info-card-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1rem;
    }

    .info-card-header i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
    }

    .info-card-header h3 {
      margin: 0;
      font-size: 1.1rem;
    }

    .tips-box {
      background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
      border: 1px solid #fbbf24;
      border-radius: 12px;
      padding: 1.5rem;
      margin: 1.5rem 0;
    }

    .tips-box h3 {
      margin-top: 0;
      color: #92400e;
    }

    .tips-box ul {
      margin: 0.5rem 0 0 0;
      color: #78350f;
    }

    .feature-section {
      margin: 2rem 0;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .stat-item {
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-radius: 8px;
      padding: 1rem;
      text-align: center;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .stat-item i {
      font-size: 2rem;
      color: var(--primary-color, #dc2626);
    }

    .stat-item strong {
      font-size: 0.95rem;
      color: var(--text-color, #1f2937);
    }

    .stat-item span {
      font-size: 0.85rem;
      color: var(--text-color, #6b7280);
    }

    .filter-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin: 1rem 0;
    }

    .filter-item {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 3px solid var(--primary-color, #dc2626);
    }

    .filter-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .filter-item strong {
      display: block;
      margin-bottom: 0.25rem;
    }

    .filter-item p {
      margin: 0;
      font-size: 0.9rem;
    }

    .steps-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin: 1rem 0;
    }

    .step-box {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 4px solid var(--primary-color, #dc2626);
    }

    .step-number {
      background: var(--primary-color, #dc2626);
      color: white;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      flex-shrink: 0;
    }

    .step-text {
      flex: 1;
    }

    .feature-list {
      list-style: none;
      padding: 0;
      margin: 1rem 0;
    }

    .feature-list li {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.75rem;
      margin-bottom: 0.5rem;
      background: var(--background-color, #f9fafb);
      border-radius: 6px;
    }

    .feature-list li i {
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .example-boxes {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .example-box {
      background: linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%);
      border: 1px solid var(--primary-color, #fca5a5);
      border-radius: 8px;
      padding: 1rem;
      font-style: italic;
      color: var(--text-color, #7f1d1d);
      text-align: center;
    }

    .quick-start {
      margin: 2rem 0;
    }

    .steps-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      margin: 1.5rem 0;
    }

    .step-item {
      display: flex;
      gap: 1.5rem;
      align-items: flex-start;
    }

    .step-item .step-number {
      width: 40px;
      height: 40px;
      font-size: 1.1rem;
    }

    .step-content h4 {
      margin: 0 0 0.5rem 0;
      color: var(--text-color, #1f2937);
    }

    .step-content p {
      margin: 0;
      color: var(--text-color, #6b7280);
    }

    .feature-icon {
      width: 60px;
      height: 60px;
      background: linear-gradient(135deg, var(--primary-color, #dc2626) 0%, var(--primary-color-strong, #b91c1c) 100%);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1rem;
    }

    .feature-icon i {
      font-size: 2rem;
      color: white;
    }

    .info-box {
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-left: 4px solid var(--primary-color, #dc2626);
      border-radius: 8px;
      padding: 1.25rem;
      margin: 1rem 0;
    }

    .info-box-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.75rem;
    }

    .info-box-header i {
      color: var(--primary-color, #dc2626);
    }

    .quick-info {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .quick-info-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .quick-info-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .format-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.25rem;
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-radius: 20px;
      margin: 1rem 0;
    }

    .format-badge i {
      color: var(--primary-color, #dc2626);
    }

    .theme-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .theme-item {
      text-align: center;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .theme-color {
      width: 100%;
      height: 60px;
      border-radius: 8px;
      margin-bottom: 0.75rem;
      border: 2px solid var(--secondary-color, #e5e7eb);
    }

    .theme-item strong {
      display: block;
      margin-bottom: 0.25rem;
    }

    .theme-item span {
      font-size: 0.85rem;
      color: var(--text-color, #6b7280);
    }

    .note-text {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 3px solid var(--primary-color, #dc2626);
      margin: 1rem 0;
    }

    .note-text i {
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .task-types {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin: 1rem 0;
    }

    .task-type-item {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 4px solid var(--primary-color, #dc2626);
    }

    .task-type-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .priority-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin: 1rem 0;
    }

    .priority-item {
      display: flex;
      gap: 1rem;
      align-items: flex-start;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
    }

    .priority-badge {
      width: 20px;
      height: 20px;
      border-radius: 50%;
      flex-shrink: 0;
      margin-top: 0.25rem;
    }

    .priority-high .priority-badge {
      background: #dc2626;
    }

    .priority-medium .priority-badge {
      background: #f59e0b;
    }

    .priority-low .priority-badge {
      background: #3b82f6;
    }

    .auto-tasks {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .auto-task-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .auto-task-item i {
      color: var(--primary-color, #dc2626);
      font-size: 1.25rem;
    }

    .security-features {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .security-item {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .security-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .bank-features {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .bank-feature-item {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 4px solid var(--primary-color, #dc2626);
    }

    .bank-feature-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
    }

    .reconciliation-methods {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .method-item {
      text-align: center;
      padding: 1.5rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border: 1px solid var(--secondary-color, #e5e7eb);
    }

    .method-item i {
      font-size: 2.5rem;
      color: var(--primary-color, #dc2626);
      margin-bottom: 1rem;
    }

    .method-item strong {
      display: block;
      margin-bottom: 0.5rem;
      font-size: 1rem;
    }

    .method-item p {
      font-size: 0.9rem;
      margin: 0;
    }

    .requirement-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin: 1rem 0;
    }

    .requirement-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 4px solid var(--primary-color, #dc2626);
    }

    .requirement-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
    }

    .setup-step {
      margin: 2rem 0;
      background: var(--background-color, #f9fafb);
      border: 1px solid var(--secondary-color, #e5e7eb);
      border-radius: 12px;
      overflow: hidden;
    }

    .setup-step-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.25rem;
      background: linear-gradient(135deg, var(--primary-color, #dc2626) 0%, var(--primary-color-strong, #b91c1c) 100%);
      color: white;
    }

    .setup-step-header .step-number {
      background: white;
      color: var(--primary-color, #dc2626);
      width: 40px;
      height: 40px;
      font-size: 1.1rem;
    }

    .setup-step-header h4 {
      margin: 0;
      color: white;
      font-size: 1.1rem;
    }

    .setup-step-content {
      padding: 1.5rem;
    }

    .chat-capabilities {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
      margin: 1rem 0;
    }

    .capability-item {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      background: var(--background-color, #f9fafb);
      border-radius: 8px;
      border-left: 4px solid var(--primary-color, #dc2626);
    }

    .capability-item i {
      font-size: 1.5rem;
      color: var(--primary-color, #dc2626);
      margin-top: 0.25rem;
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

      .info-grid {
        grid-template-columns: 1fr;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      .example-boxes {
        grid-template-columns: 1fr;
      }

      .theme-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .intro-banner {
        padding: 1.5rem;
      }

      .intro-banner h1 {
        font-size: 1.4rem;
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
