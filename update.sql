BEGIN TRANSACTION;
GO

CREATE TABLE [SistemGuncellemeleri] (
    [Id] int NOT NULL IDENTITY,
    [Versiyon] nvarchar(20) NOT NULL,
    [Baslik] nvarchar(100) NOT NULL,
    [Icerik] nvarchar(max) NOT NULL,
    [EklenmeTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_SistemGuncellemeleri] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260910084800_AddSistemGuncellemeleri', N'8.0.8');
GO

COMMIT;
GO

