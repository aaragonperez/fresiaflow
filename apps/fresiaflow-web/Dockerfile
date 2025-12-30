# Dockerfile para FresiaFlow Web (Angular)
# Etapa 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Copiar archivos de dependencias
COPY package*.json ./
RUN npm ci

# Copiar código fuente
COPY . .

# Build de la aplicación Angular
RUN npm run build

# Etapa 2: Servir con nginx
FROM nginx:alpine

# Copiar archivos construidos
# Angular 17 puede generar los archivos en dist/fresiaflow-web/browser o directamente en dist/fresiaflow-web
# Usamos un script para manejar ambos casos
COPY --from=build /app/dist/fresiaflow-web /tmp/dist

# Mover archivos a la ubicación correcta (manejar subdirectorio browser si existe)
RUN if [ -d /tmp/dist/browser ]; then \
        cp -r /tmp/dist/browser/* /usr/share/nginx/html/; \
    else \
        cp -r /tmp/dist/* /usr/share/nginx/html/; \
    fi && \
    rm -rf /tmp/dist

# Copiar configuración de nginx personalizada
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

