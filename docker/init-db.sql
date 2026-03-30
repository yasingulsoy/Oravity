-- Oravity geliştirme ortamı — ek veritabanı oluşturma
-- PostgreSQL container ilk çalıştığında bu script çalışır

CREATE DATABASE oravity_backend_dev
    WITH OWNER = oravity
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TEMPLATE = template0;

-- Hangfire için ayrı schema (oravity_backend_dev içinde otomatik oluşur)
-- EF Core migration'ları çalıştırıldığında tablolar oluşturulacak
