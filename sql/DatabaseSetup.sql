CREATE DATABASE [LocationTracker]
GO
USE [LocationTracker]
GO
IF NOT EXISTS (SELECT name FROM sys.filegroups WHERE is_default=1 AND name = N'PRIMARY') ALTER DATABASE [LocationTracker] MODIFY FILEGROUP [PRIMARY] DEFAULT
GO
CREATE TABLE dbo.LocationLog
(
	Id int IDENTITY NOT NULL,
	PointerName nvarchar(30) NOT NULL,
	Latitude decimal(18, 12) NOT NULL,
	Longitude decimal(18, 12) NOT NULL
)  ON [PRIMARY]
GO
ALTER TABLE dbo.LocationLog ADD CONSTRAINT
PK_LocationLog PRIMARY KEY CLUSTERED 
(
	Id
)
GO
USE master;
GO
ALTER DATABASE [LocationTracker]
    SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
GO
USE [LocationTracker];
GO