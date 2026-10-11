FROM node:22-bookworm-slim AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --legacy-peer-deps
COPY . .
ENV NODE_OPTIONS=--openssl-legacy-provider
ENV VUE_APP_BACKEND_URL=http://localhost:8099/api/
ENV VUE_APP_AUTH_URL=http://localhost:6060/api/
ENV VUE_APP_CHAT_URL=http://localhost:4099
RUN npm run build
FROM nginx:stable
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 80
