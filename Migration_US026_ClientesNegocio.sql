-- US-026. La API consulta el historial desde citas y crea fichas cuando se guardan.
BEGIN;

SET LOCAL lock_timeout = '2s';
SET LOCAL statement_timeout = '15s';

CREATE TABLE public.clientes_negocio (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_negocio UUID NOT NULL REFERENCES public.negocios(id) ON DELETE CASCADE,
    id_cliente UUID REFERENCES public.clientes(id) ON DELETE SET NULL,
    nombre VARCHAR(100) NOT NULL,
    apellido VARCHAR(100) NOT NULL DEFAULT '',
    correo VARCHAR(150) NOT NULL DEFAULT '',
    telefono VARCHAR(30),
    notas VARCHAR(1000),
    fecha_creacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_actualizacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_clientes_negocio_negocio
    ON public.clientes_negocio (id_negocio, id);

CREATE INDEX idx_clientes_negocio_negocio_cliente
    ON public.clientes_negocio (id_negocio, id_cliente);

-- El acceso a las fichas se realiza desde el backend, con el negocio del JWT.
ALTER TABLE public.clientes_negocio ENABLE ROW LEVEL SECURITY;
REVOKE ALL PRIVILEGES ON TABLE public.clientes_negocio FROM PUBLIC, anon, authenticated, service_role;

COMMIT;
