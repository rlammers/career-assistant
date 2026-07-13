FROM node:26-alpine AS build
WORKDIR /app

COPY src/frontend/package*.json ./
RUN npm ci

COPY src/frontend/ ./

ARG VITE_API_BASE_URL=/api
ARG VITE_AUTH_ENABLED=false
ARG VITE_ENTRA_TENANT_ID=
ARG VITE_ENTRA_SPA_CLIENT_ID=
ARG VITE_ENTRA_API_SCOPE=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_AUTH_ENABLED=$VITE_AUTH_ENABLED
ENV VITE_ENTRA_TENANT_ID=$VITE_ENTRA_TENANT_ID
ENV VITE_ENTRA_SPA_CLIENT_ID=$VITE_ENTRA_SPA_CLIENT_ID
ENV VITE_ENTRA_API_SCOPE=$VITE_ENTRA_API_SCOPE

RUN npm run build

FROM nginxinc/nginx-unprivileged:1.31.2-alpine AS runtime
USER root
RUN apk upgrade --no-cache
USER 101
ENV API_UPSTREAM=http://backend:8080
COPY src/frontend/nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
