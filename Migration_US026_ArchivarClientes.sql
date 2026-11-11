BEGIN;

SET LOCAL lock_timeout = '2s';
SET LOCAL statement_timeout = '15s';

ALTER TABLE public.clientes_negocio
    ADD COLUMN IF NOT EXISTS fecha_archivado TIMESTAMP WITH TIME ZONE,
    ADD COLUMN IF NOT EXISTS citas_historial UUID[] NOT NULL DEFAULT '{}'::UUID[];

COMMIT;
