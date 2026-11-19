-- Supabase Storage Bucket & RLS Policies for Slotify business logos
-- Execute in Supabase SQL Editor if your database migration user lacks ownership of storage.objects.

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'business-logos',
    'business-logos',
    TRUE,
    4194304,
    ARRAY['image/jpeg', 'image/png', 'image/webp']
)
ON CONFLICT (id) DO UPDATE
SET public = TRUE,
    file_size_limit = 4194304,
    allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/webp'];

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Public read business-logos'
    ) THEN
        CREATE POLICY "Public read business-logos"
        ON storage.objects FOR SELECT
        TO public
        USING (bucket_id = 'business-logos');
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated upload own folder in business-logos'
    ) THEN
        CREATE POLICY "Authenticated upload own folder in business-logos"
        ON storage.objects FOR INSERT
        TO authenticated
        WITH CHECK (
            bucket_id = 'business-logos'
            AND (storage.foldername(name))[1] = auth.uid()::text
        );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated update own folder in business-logos'
    ) THEN
        CREATE POLICY "Authenticated update own folder in business-logos"
        ON storage.objects FOR UPDATE
        TO authenticated
        USING (
            bucket_id = 'business-logos'
            AND (storage.foldername(name))[1] = auth.uid()::text
        );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated delete own folder in business-logos'
    ) THEN
        CREATE POLICY "Authenticated delete own folder in business-logos"
        ON storage.objects FOR DELETE
        TO authenticated
        USING (
            bucket_id = 'business-logos'
            AND (storage.foldername(name))[1] = auth.uid()::text
        );
    END IF;
END $$;
