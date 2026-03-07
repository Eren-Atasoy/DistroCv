# =============================================================================
# DistroCv — Makefile (Koyeb)
# =============================================================================
.PHONY: help local-setup local-up local-down local-rebuild local-logs \
        local-restart-api local-status db-migrate db-shell \
        prod-deploy prod-deploy-api prod-deploy-frontend \
        prod-logs prod-status clean

# Varsayılan hedef
help:
	@echo ""
	@echo "  DistroCv — Ortam Komutları"
	@echo "  ============================="
	@echo ""
	@echo "  LOCAL:"
	@echo "    make local-setup          → .env.local dosyasını oluştur (ilk kurulum)"
	@echo "    make local-up             → Local servisleri başlat"
	@echo "    make local-down           → Local servisleri durdur"
	@echo "    make local-rebuild        → API'yi yeniden build et ve başlat"
	@echo "    make local-logs           → API loglarını takip et"
	@echo "    make local-status         → Servis durumlarını göster"
	@echo ""
	@echo "  PRODUCTION (Koyeb):"
	@echo "    make prod-deploy          → API + Frontend'i redeploy et"
	@echo "    make prod-deploy-api      → Sadece API'yi redeploy et"
	@echo "    make prod-deploy-frontend → Sadece Frontend'i redeploy et"
	@echo "    make prod-logs            → Production API logları"
	@echo "    make prod-status          → Koyeb servis durumu"
	@echo ""
	@echo "  DATABASE:"
	@echo "    make db-migrate           → SQL migration uygula (local)"
	@echo "    make db-shell             → PostgreSQL shell (local)"
	@echo ""
	@echo "  DİĞER:"
	@echo "    make clean                → Container + volume temizle"
	@echo ""

# =============================================================================
# LOCAL
# =============================================================================

local-setup:
	@if [ ! -f .env.local ]; then \
		cp .env.local.example .env.local; \
		echo "✅ .env.local oluşturuldu. Değerleri doldurmayı unutma!"; \
	else \
		echo "ℹ️  .env.local zaten mevcut, atlandı."; \
	fi
	@if [ ! -f client/.env.local ]; then \
		echo "VITE_API_URL=http://localhost:5000/api" > client/.env.local; \
		echo "✅ client/.env.local oluşturuldu."; \
	fi

local-up: local-setup
	@echo "🚀 Local ortam başlatılıyor..."
	docker compose -f docker-compose.yml -f docker-compose.local.yml --env-file .env.local up -d
	@echo ""
	@echo "✅ Servisler hazır:"
	@echo "   Frontend  : http://localhost:5173"
	@echo "   API       : http://localhost:5000"
	@echo "   Scalar UI : http://localhost:5000/scalar/v1"
	@echo "   pgAdmin   : http://localhost:5050"
	@echo "   PostgreSQL: localhost:5432"
	@echo "   Redis     : localhost:6380"
	@echo ""

local-down:
	@echo "🛑 Local ortam durduruluyor..."
	docker compose -f docker-compose.yml -f docker-compose.local.yml down

local-rebuild:
	@echo "🔨 API yeniden build ediliyor..."
	docker compose -f docker-compose.yml -f docker-compose.local.yml --env-file .env.local up -d --build api
	@echo "✅ API yeniden başlatıldı."

local-logs:
	docker compose -f docker-compose.yml -f docker-compose.local.yml logs -f api

local-logs-all:
	docker compose -f docker-compose.yml -f docker-compose.local.yml logs -f

local-restart-api:
	docker compose -f docker-compose.yml -f docker-compose.local.yml restart api

local-status:
	docker compose -f docker-compose.yml -f docker-compose.local.yml ps

# =============================================================================
# PRODUCTION — KOYEB
# .env.prod dosyasındaki KOYEB_* değişkenleri kullanılır.
# =============================================================================

# .env.prod load is deferred to individual prod commands to avoid conflicting with local environment
# ifneq (,$(wildcard .env.prod))
#   include .env.prod
#   export
# endif

prod-deploy: prod-deploy-api prod-deploy-frontend
	@echo ""
	@echo "✅ Production deploy tamamlandı!"
	@echo "   API      : https://$(KOYEB_APP_NAME)-api-$(shell koyeb service get $(KOYEB_APP_NAME)/$(KOYEB_API_SERVICE_NAME) -o json 2>/dev/null | grep -o 'koyeb.app[^\"]*' | head -1 || echo 'koyeb.app')"
	@echo ""

prod-deploy-api:
	@echo "🚀 Koyeb API redeploy başlatılıyor..."
	@command -v koyeb >/dev/null 2>&1 || { echo "❌ Koyeb CLI kurulu değil. Kur: curl -fsSL https://raw.githubusercontent.com/koyeb/koyeb-cli/master/install.sh | sh"; exit 1; }
	KOYEB_TOKEN=$(KOYEB_API_TOKEN) koyeb service redeploy $(KOYEB_APP_NAME)/$(KOYEB_API_SERVICE_NAME)
	@echo "✅ API deploy başlatıldı."

prod-deploy-frontend:
	@echo "🌐 Koyeb Frontend redeploy başlatılıyor..."
	@command -v koyeb >/dev/null 2>&1 || { echo "❌ Koyeb CLI kurulu değil. Kur: curl -fsSL https://raw.githubusercontent.com/koyeb/koyeb-cli/master/install.sh | sh"; exit 1; }
	KOYEB_TOKEN=$(KOYEB_API_TOKEN) koyeb service redeploy $(KOYEB_APP_NAME)/$(KOYEB_FRONTEND_SERVICE_NAME)
	@echo "✅ Frontend deploy başlatıldı."

prod-logs:
	@echo "📋 Production API logları:"
	KOYEB_TOKEN=$(KOYEB_API_TOKEN) koyeb service logs $(KOYEB_APP_NAME)/$(KOYEB_API_SERVICE_NAME) --tail

prod-status:
	@echo "📊 Koyeb servis durumu:"
	KOYEB_TOKEN=$(KOYEB_API_TOKEN) koyeb service list

prod-health:
	@echo "🔍 Health check..."
	@STATUS=$$(curl -s -o /dev/null -w "%{http_code}" \
		https://$(KOYEB_APP_NAME)-api-$(KOYEB_API_SERVICE_NAME).koyeb.app/health); \
	echo "Status: $$STATUS"; \
	[ "$$STATUS" = "200" ] && echo "✅ API sağlıklı!" || echo "❌ API yanıt vermiyor!"

# =============================================================================
# DATABASE
# =============================================================================

db-migrate:
	@echo "🗃️  Migration'lar uygulanıyor (local)..."
	docker exec -i distrocv-postgres psql -U $${POSTGRES_USER:-postgres} -d $${POSTGRES_DB:-distrocv_dev} < migrations.sql
	@echo "✅ Migration tamamlandı."

db-shell:
	docker exec -it distrocv-postgres psql -U $${POSTGRES_USER:-postgres} -d $${POSTGRES_DB:-distrocv_dev}

# =============================================================================
# UTILITIES
# =============================================================================

clean:
	@echo "🧹 Tüm container ve volume'lar siliniyor..."
	docker compose -f docker-compose.yml -f docker-compose.local.yml down -v --remove-orphans
	@echo "✅ Temizlendi."
