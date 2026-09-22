-- Añadir la columna de configuración de formulario personalizada a la tabla negocios
ALTER TABLE negocios 
ADD COLUMN IF NOT EXISTS configuracion_formulario_personalizada JSONB;
