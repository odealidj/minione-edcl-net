-- =============================================================================
-- Database Initialization Script for EDCL Telematics & GPS Tracking Service
-- Database Name : AE031_EDCL_GPS_DB
-- Target Engine : PostgreSQL 15+ / 16 (Local Podman & AWS RDS Parity)
-- Schema        : edcl
-- Author        : Antigravity & EDCL Engineering Team
-- =============================================================================

-- 1. Create Schema
CREATE SCHEMA IF NOT EXISTS edcl;

-- Set search path
SET search_path TO edcl, public;

-- 2. Master Tables

-- 2.1 Master GPS Vendor
CREATE TABLE IF NOT EXISTS edcl.tb_m_gps_vendor (
    "Id" UUID NOT NULL,
    "VendorName" VARCHAR(50) NOT NULL,
    "Timezone" VARCHAR(50) DEFAULT 'Asia/Jakarta',
    "RequiredAuth" BOOLEAN DEFAULT false,
    "AuthType" VARCHAR(20) DEFAULT 'NoAuth',
    "Username" VARCHAR(100) NULL,
    "Password" VARCHAR(255) NULL,
    "ProcessingStrategy" VARCHAR(50) DEFAULT 'Individual',
    "ProcessingStrategyPathData" VARCHAR(255) NULL,
    "ProcessingStrategyPathKey" VARCHAR(255) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_gps_vendor PRIMARY KEY ("Id")
);

-- 2.2 Master GPS Vendor Endpoint
CREATE TABLE IF NOT EXISTS edcl.tb_m_gps_vendor_endpoint (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "BaseUrl" VARCHAR(500) NOT NULL,
    "Method" VARCHAR(10) NOT NULL,
    "ContentType" VARCHAR(50) DEFAULT 'application/json',
    "Headers" JSONB NULL,
    "Params" JSONB NULL,
    "Bodies" JSONB NULL,
    "VarParams" JSONB NULL,
    "MaxPath" VARCHAR(255) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_gps_vendor_endpoint PRIMARY KEY ("Id"),
    CONSTRAINT fk_endpoint_vendor FOREIGN KEY ("GpsVendorId") REFERENCES edcl.tb_m_gps_vendor ("Id") ON DELETE CASCADE
);

-- 2.3 Master GPS Vendor Authentication
CREATE TABLE IF NOT EXISTS edcl.tb_m_gps_vendor_auth (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "BaseUrl" VARCHAR(500) NOT NULL,
    "Method" VARCHAR(10) NOT NULL,
    "Authtype" VARCHAR(50) NOT NULL,
    "ContentType" VARCHAR(50) DEFAULT 'application/json',
    "Username" VARCHAR(100) NULL,
    "Password" VARCHAR(255) NULL,
    "TokenPath" VARCHAR(255) NULL,
    "Headers" JSONB NULL,
    "Params" JSONB NULL,
    "Bodies" JSONB NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_gps_vendor_auth PRIMARY KEY ("Id"),
    CONSTRAINT fk_auth_vendor FOREIGN KEY ("GpsVendorId") REFERENCES edcl.tb_m_gps_vendor ("Id") ON DELETE CASCADE
);

-- 2.4 Master Response Field Mapping
CREATE TABLE IF NOT EXISTS edcl.tb_m_mapping (
    "Id" SERIAL NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "ResponseField" VARCHAR(100) NOT NULL,
    "MappedField" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_mapping PRIMARY KEY ("Id"),
    CONSTRAINT fk_mapping_vendor FOREIGN KEY ("GpsVendorId") REFERENCES edcl.tb_m_gps_vendor ("Id") ON DELETE CASCADE
);

-- 2.5 Master Vendor License Plate / Unit Code (LPCD)
CREATE TABLE IF NOT EXISTS edcl.tb_m_gps_vendor_lpcd (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "Lpcd" VARCHAR(10) NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_gps_vendor_lpcd PRIMARY KEY ("Id"),
    CONSTRAINT fk_lpcd_vendor FOREIGN KEY ("GpsVendorId") REFERENCES edcl.tb_m_gps_vendor ("Id") ON DELETE CASCADE
);

-- 2.6 Master System Parameters
CREATE TABLE IF NOT EXISTS edcl.tb_m_system (
    "SysCat" VARCHAR(30) NOT NULL,
    "SysSubCat" VARCHAR(255) NOT NULL,
    "SysCd" VARCHAR(30) NOT NULL,
    "SysValue" VARCHAR(255) NULL,
    "Remarks" VARCHAR(100) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_system PRIMARY KEY ("SysCat", "SysSubCat", "SysCd")
);

-- 2.7 Master GPS API Audit Log
CREATE TABLE IF NOT EXISTS edcl.tb_m_gps_api_log (
    "Id" UUID NOT NULL,
    "FunctionName" VARCHAR(255) NULL,
    "Status" VARCHAR(50) NULL,
    "ErrorMessage" TEXT NULL,
    "ErrorResponse" TEXT NULL,
    "Parameter" TEXT NULL,
    "Username" VARCHAR(100) NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITH TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_m_gps_api_log PRIMARY KEY ("Id")
);

-- 3. Transactional Tables

-- 3.1 Delivery Progress (Geofence State Tracking)
CREATE TABLE IF NOT EXISTS edcl.tb_r_delivery_progress (
    "Id" UUID NOT NULL,
    "DeliveryNo" VARCHAR(20) NOT NULL,
    "PlatNo" VARCHAR(50) NOT NULL,
    "NoKtp" VARCHAR(30) NOT NULL,
    "VendorName" VARCHAR(20) NULL,
    "Lpcd" VARCHAR(5) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_delivery_progress PRIMARY KEY ("Id")
);

-- 3.2 GPS Delivery Header
CREATE TABLE IF NOT EXISTS edcl.tb_r_gps_delivery_h (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "GpsVendorName" VARCHAR(20) NOT NULL,
    "DeliveryNo" VARCHAR(20) NULL,
    "NoKtp" VARCHAR(30) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_gps_delivery_h PRIMARY KEY ("Id")
);

-- 3.3 GPS Delivery Detail
CREATE TABLE IF NOT EXISTS edcl.tb_r_gps_delivery_d (
    "Id" UUID NOT NULL,
    "GpsDeliveryHId" UUID NULL,
    "LpcdId" UUID NULL,
    "Lpcd" VARCHAR(5) NULL,
    "PlatNo" VARCHAR(50) NULL,
    "DeviceId" VARCHAR(100) NULL,
    "Datetime" TIMESTAMP WITHOUT TIME ZONE NULL,
    "X" NUMERIC(19, 16) NULL,
    "Y" NUMERIC(19, 16) NULL,
    "Speed" NUMERIC(5) NULL,
    "Course" NUMERIC(5) NULL,
    "StreetName" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_gps_delivery_d PRIMARY KEY ("Id")
);

-- 3.4 Raw GPS Delivery (Unified History)
CREATE TABLE IF NOT EXISTS edcl.tb_r_gps_delivery (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "GpsVendorName" VARCHAR(20) NOT NULL,
    "GpsDeliveryHId" UUID NULL,
    "LpcdId" UUID NULL,
    "Lpcd" VARCHAR(5) NULL,
    "DeliveryNo" VARCHAR(20) NULL,
    "NoKtp" VARCHAR(30) NULL,
    "PlatNo" VARCHAR(50) NULL,
    "DeviceId" VARCHAR(100) NULL,
    "Datetime" TIMESTAMP WITHOUT TIME ZONE NULL,
    "X" NUMERIC(19, 16) NULL,
    "Y" NUMERIC(19, 16) NULL,
    "Speed" NUMERIC(5) NULL,
    "Course" NUMERIC(5) NULL,
    "StreetName" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_gps_delivery PRIMARY KEY ("Id")
);

-- 3.5 GPS Last Position Header
CREATE TABLE IF NOT EXISTS edcl.tb_r_gps_last_position_h (
    "Id" UUID NOT NULL,
    "GpsVendorId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_gps_last_position_h PRIMARY KEY ("Id")
);

-- 3.6 GPS Last Position Detail (Latest Coordinates Per Vehicle)
CREATE TABLE IF NOT EXISTS edcl.tb_r_gps_last_position_d (
    "Id" UUID NOT NULL,
    "GpsLastPositionHId" UUID NOT NULL,
    "Lpcd" VARCHAR(5) NULL,
    "PlatNo" VARCHAR(50) NULL,
    "DeviceId" VARCHAR(100) NULL,
    "Datetime" TIMESTAMP WITHOUT TIME ZONE NULL,
    "X" NUMERIC(19, 16) NULL,
    "Y" NUMERIC(19, 16) NULL,
    "Speed" NUMERIC(5) NULL,
    "Course" NUMERIC(5) NULL,
    "StreetName" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(20) DEFAULT 'System',
    "LastModified" TIMESTAMP WITHOUT TIME ZONE NULL,
    "LastModifiedBy" VARCHAR(20) NULL,
    CONSTRAINT pk_tb_r_gps_last_position_d PRIMARY KEY ("Id"),
    CONSTRAINT fk_last_position_h FOREIGN KEY ("GpsLastPositionHId") REFERENCES edcl.tb_r_gps_last_position_h ("Id") ON DELETE CASCADE
);

-- 4. Indexes for Performance Tuning
CREATE INDEX IF NOT EXISTS idx_gps_last_pos_d_platno ON edcl.tb_r_gps_last_position_d ("PlatNo");
CREATE INDEX IF NOT EXISTS idx_gps_last_pos_d_datetime ON edcl.tb_r_gps_last_position_d ("Datetime" DESC);
CREATE INDEX IF NOT EXISTS idx_delivery_progress_deliveryno ON edcl.tb_r_delivery_progress ("DeliveryNo");
CREATE INDEX IF NOT EXISTS idx_delivery_progress_platno ON edcl.tb_r_delivery_progress ("PlatNo");
CREATE INDEX IF NOT EXISTS idx_gps_delivery_d_platno ON edcl.tb_r_gps_delivery_d ("PlatNo");
CREATE INDEX IF NOT EXISTS idx_gps_delivery_datetime ON edcl.tb_r_gps_delivery ("Datetime" DESC);

-- 5. Seed Initial Vendor Master & Config Data
INSERT INTO edcl.tb_m_gps_vendor ("Id", "VendorName", "Timezone", "RequiredAuth", "AuthType", "ProcessingStrategy", "CreatedBy")
VALUES 
    ('a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d', 'HINO-CONNECT', 'Asia/Jakarta', false, 'NoAuth', 'Individual', 'SeedAdmin'),
    ('b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e', 'JITRA-GPS', 'Asia/Jakarta', false, 'NoAuth', 'Individual', 'SeedAdmin'),
    ('c3d4e5f6-a7b8-9c0d-1e2f-3a4b5c6d7e8f', 'PUNINAR-TELEMATICS', 'Asia/Jakarta', false, 'NoAuth', 'Individual', 'SeedAdmin')
ON CONFLICT ("Id") DO NOTHING;

-- Seed Master System Parameters (Polling intervals, geofence threshold)
INSERT INTO edcl.tb_m_system ("SysCat", "SysSubCat", "SysCd", "SysValue", "Remarks", "CreatedBy")
VALUES
    ('GEOFENCE', 'INTERVAL', 'POLLING_SEC', '30', 'Interval polling vendor GPS (detik)', 'SeedAdmin'),
    ('GEOFENCE', 'RADIUS', 'SUPPLIER_METER', '100', 'Radius geofence supplier dalam meter', 'SeedAdmin'),
    ('GEOFENCE', 'RADIUS', 'PLANT_METER', '150', 'Radius geofence pabrik Toyota dalam meter', 'SeedAdmin'),
    ('RABBITMQ', 'TOPIC', 'EXCHANGE_NAME', 'topic_exchange', 'Exchange RabbitMQ untuk broadcast GPS', 'SeedAdmin')
ON CONFLICT ("SysCat", "SysSubCat", "SysCd") DO NOTHING;

-- Seed Sample Trucks matching EDCL Mini Driver Fleet
INSERT INTO edcl.tb_m_gps_vendor_lpcd ("Id", "GpsVendorId", "Lpcd", "CreatedBy")
VALUES
    ('d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f90', 'a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d', 'TMM01', 'SeedAdmin'),
    ('e5f6a7b8-c9d0-1e2f-3a4b-5c6d7e8f9012', 'a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d', 'TYT02', 'SeedAdmin')
ON CONFLICT ("Id") DO NOTHING;
