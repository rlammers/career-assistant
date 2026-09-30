ARG SOURCE_REVISION=local
ARG SOURCE_URL=local

FROM node:26-alpine@sha256:0b36e8c136b94cd4fcf02188228e76c31ad5872eef3fec8cbd2eee500cfd9e80 AS build
WORKDIR /app

COPY src/frontend/package*.json ./
RUN npm ci

COPY src/frontend/tsconfig*.json src/frontend/vite.config.ts src/frontend/index.html ./
COPY src/frontend/public/ ./public/
COPY src/frontend/src/ ./src/

RUN test ! -e /app/.env.local

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

FROM nginxinc/nginx-unprivileged:1.31.6-alpine@sha256:26b0bf6fbf07297983cb341998d79c831508787de26627dd2a112321b9c3a4af AS runtime
ARG SOURCE_REVISION
ARG SOURCE_URL
LABEL org.opencontainers.image.revision="${SOURCE_REVISION}" \
      org.opencontainers.image.source="${SOURCE_URL}"
USER root
RUN apk upgrade --no-cache
USER 101
ENV API_UPSTREAM=http://backend:8080
COPY src/frontend/nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
