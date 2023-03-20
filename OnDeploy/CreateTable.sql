USE [AdvertisementNF]
GO

/****** Object:  Table [dbo].[IAccessMatrix]    Script Date: 03.04.2022 17:27:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[IAccessMatrix](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[AccessDescribe] [varchar](100) NOT NULL,
	[AccessName] [varchar](50) NOT NULL,
	[AccessGrant] [varchar](1000) NOT NULL,
 CONSTRAINT [PK_IAccessMatrix] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[IAccessMatrix] ADD  CONSTRAINT [DF_IAccessMatrix_AccessGrant]  DEFAULT ('') FOR [AccessGrant]
GO

GRANT SELECT, UPDATE, INSERT ON IAccessMatrix TO RTUsers;

INSERT INTO IAccessMatrix (AccessDescribe, AccessName) VALUES('Включить в список "Дизайнеры"', 'ListDesigner')
INSERT INTO IAccessMatrix (AccessDescribe, AccessName) VALUES('Включить в список "Менеджеры"', 'ListManager')


