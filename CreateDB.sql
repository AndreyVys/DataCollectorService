USE [master]
GO

CREATE DATABASE [briquettes]
GO

USE [briquettes]
GO

CREATE LOGIN scada   
    WITH PASSWORD = 'scada', DEFAULT_DATABASE=[briquettes];  
GO 

CREATE USER scada FOR LOGIN scada
	WITH DEFAULT_SCHEMA=[db_datawriter]; 
GO

ALTER ROLE db_datawriter ADD MEMBER [scada]
GO


CREATE TABLE [dbo].[Press1Data](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[datetime] [datetime] NOT NULL,
	[Error] [bit] NULL,
	[PressOff] [bit] NULL,
	[ManualMode] [bit] NULL,
	[AutoMode] [bit] NULL,
	[Counter] [bigint] NULL,
	[MotorHours] [bigint] NULL,
	[Safety] [bit] NULL,
	[DirtyFilter] [bit] NULL,
	[OilLevel] [bit] NULL,
	[OilLess15] [bit] NULL,
	[OilMore75] [bit] NULL,
	[PressureSensorError] [bit] NULL,
	[TempSensorError] [bit] NULL,
	[CylindersNotInHome] [bit] NULL,
	[MainCylinderPressureError] [bit] NULL,
	[VerticalCylinderPressureError] [bit] NULL,
	[FormCylinderPressureError] [bit] NULL,
	[FreeStrokeMainCylinder] [bit] NULL,
	[FreeStrokeVerticalCylinder] [bit] NULL,
	[FreeStrokeFormCylinder] [bit] NULL,
	[NoSawdust] [bit] NULL,
	[Lifebit] [bit] NULL,
 CONSTRAINT [PK_Press1Data] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Press2Data](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[datetime] [datetime] NOT NULL,
	[Error] [bit] NULL,
	[PressOff] [bit] NULL,
	[ManualMode] [bit] NULL,
	[AutoMode] [bit] NULL,
	[Counter] [bigint] NULL,
	[MotorHours] [bigint] NULL,
	[Safety] [bit] NULL,
	[DirtyFilter] [bit] NULL,
	[OilLevel] [bit] NULL,
	[OilLess15] [bit] NULL,
	[OilMore75] [bit] NULL,
	[PressureSensorError] [bit] NULL,
	[TempSensorError] [bit] NULL,
	[CylindersNotInHome] [bit] NULL,
	[MainCylinderPressureError] [bit] NULL,
	[VerticalCylinderPressureError] [bit] NULL,
	[FormCylinderPressureError] [bit] NULL,
	[FreeStrokeMainCylinder] [bit] NULL,
	[FreeStrokeVerticalCylinder] [bit] NULL,
	[FreeStrokeFormCylinder] [bit] NULL,
	[NoSawdust] [bit] NULL,
	[Lifebit] [bit] NULL,
 CONSTRAINT [PK_Press2Data] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Press3Data](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[datetime] [datetime] NOT NULL,
	[Error] [bit] NULL,
	[PressOff] [bit] NULL,
	[ManualMode] [bit] NULL,
	[AutoMode] [bit] NULL,
	[Counter] [bigint] NULL,
	[MotorHours] [bigint] NULL,
	[Safety] [bit] NULL,
	[DirtyFilter] [bit] NULL,
	[OilLevel] [bit] NULL,
	[OilLess15] [bit] NULL,
	[OilMore75] [bit] NULL,
	[PressureSensorError] [bit] NULL,
	[TempSensorError] [bit] NULL,
	[CylindersNotInHome] [bit] NULL,
	[MainCylinderPressureError] [bit] NULL,
	[VerticalCylinderPressureError] [bit] NULL,
	[FormCylinderPressureError] [bit] NULL,
	[FreeStrokeMainCylinder] [bit] NULL,
	[FreeStrokeVerticalCylinder] [bit] NULL,
	[FreeStrokeFormCylinder] [bit] NULL,
	[NoSawdust] [bit] NULL,
	[Lifebit] [bit] NULL,
 CONSTRAINT [PK_Press3Data] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



