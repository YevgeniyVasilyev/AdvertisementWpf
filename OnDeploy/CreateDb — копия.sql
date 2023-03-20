USE [master]
GO

DECLARE @DbName VARCHAR(50), @DbPath VARCHAR(200), @LDFName VARCHAR(50), @SQL VARCHAR(MAX)
SET @DbName = N'AdvertisementNF'
SET @DbPath = N'D:\SQLDatabases\'

SET @SQL = 'CREATE DATABASE ' + @DbName
SET @SQL = @SQL + ' CONTAINMENT = NONE 
 ON  PRIMARY 
( NAME = [' + @DbName + '], FILENAME = [' + @DbPath + @DbName + '.mdf] , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = [' + @DbName + '_log], FILENAME = [' + @DbPath + @DbName + '.ldf] , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT'
EXEC (@SQL)

SET @SQL = ' IF (1 = FULLTEXTSERVICEPROPERTY(''IsFullTextInstalled''))
begin
EXEC [@DbName].[dbo].[sp_fulltext_database] @action = ''enable''
end
ALTER DATABASE [@DbName] SET ANSI_NULL_DEFAULT OFF 
ALTER DATABASE [@DbName] SET ANSI_NULLS OFF 
ALTER DATABASE [@DbName] SET ANSI_PADDING OFF 
ALTER DATABASE [@DbName] SET ANSI_WARNINGS OFF 
ALTER DATABASE [@DbName] SET ARITHABORT OFF 
ALTER DATABASE [@DbName] SET AUTO_CLOSE OFF 
ALTER DATABASE [@DbName] SET AUTO_SHRINK OFF 
ALTER DATABASE [@DbName] SET AUTO_UPDATE_STATISTICS OFF 
ALTER DATABASE [@DbName] SET CURSOR_CLOSE_ON_COMMIT OFF 
ALTER DATABASE [@DbName] SET CURSOR_DEFAULT  GLOBAL 
ALTER DATABASE [@DbName] SET CONCAT_NULL_YIELDS_NULL OFF 
ALTER DATABASE [@DbName] SET NUMERIC_ROUNDABORT OFF 
ALTER DATABASE [@DbName] SET QUOTED_IDENTIFIER OFF 
ALTER DATABASE [@DbName] SET RECURSIVE_TRIGGERS OFF 
ALTER DATABASE [@DbName] SET  DISABLE_BROKER 
ALTER DATABASE [@DbName] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
ALTER DATABASE [@DbName] SET DATE_CORRELATION_OPTIMIZATION OFF 
ALTER DATABASE [@DbName] SET TRUSTWORTHY OFF 
ALTER DATABASE [@DbName] SET ALLOW_SNAPSHOT_ISOLATION OFF 
ALTER DATABASE [@DbName] SET PARAMETERIZATION SIMPLE 
ALTER DATABASE [@DbName] SET READ_COMMITTED_SNAPSHOT OFF 
ALTER DATABASE [@DbName] SET HONOR_BROKER_PRIORITY OFF 
ALTER DATABASE [@DbName] SET RECOVERY SIMPLE 
ALTER DATABASE [@DbName] SET  MULTI_USER 
ALTER DATABASE [@DbName] SET PAGE_VERIFY CHECKSUM  
ALTER DATABASE [@DbName] SET DB_CHAINING OFF 
ALTER DATABASE [@DbName] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
ALTER DATABASE [@DbName] SET TARGET_RECOVERY_TIME = 60 SECONDS 
ALTER DATABASE [@DbName] SET DELAYED_DURABILITY = DISABLED 
ALTER DATABASE [@DbName] SET ACCELERATED_DATABASE_RECOVERY = OFF  
ALTER DATABASE [@DbName] SET QUERY_STORE = OFF
ALTER DATABASE [@DbName] SET  READ_WRITE' 
SET  @SQL = REPLACE (@SQL ,  '[@DbName]' ,  @DbName)

EXEC (@SQL)

SET @SQL = 'USE [' + @DbName + ']'
EXEC (@SQL)
CREATE ROLE RTUsers AUTHORIZATION db_securityadmin; 



SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Roles](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[RoleName] [varchar](50) NOT NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Users](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[FirstName] [varchar](30) NOT NULL,
	[LastName] [varchar](30) NULL,
	[MiddleName] [varchar](30) NULL,
	[LoginName] [varchar](20) NOT NULL,
	[RoleID] [bigint] NOT NULL,
	[Disabled] [bit] NOT NULL,
	[CategoryWork] [smallint] NOT NULL,
	[IsAdmin] [bit] NOT NULL,
	[IsExternal] [bit] NOT NULL,
	[Phone] [varchar](12) NULL,
	[Email] [varchar](50) NULL,
	[CardNumber] [varchar](16) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO

ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_IsAdmin]  DEFAULT ((0)) FOR [IsAdmin]
GO

ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_IsExternal]  DEFAULT ((0)) FOR [IsExternal]
GO

ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Roles] FOREIGN KEY([RoleID])
REFERENCES [dbo].[Roles] ([ID])
GO

ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Roles]
GO

CREATE TRIGGER [dbo].[OnInsertUpdateUsers] ON [dbo].[Users] AFTER INSERT, UPDATE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @ExecString VARCHAR(1000), @NewLoginName VARCHAR(200), @CurrentLoginName VARCHAR(200), @UserID BIGINT, @NewDisabled BIT, @IsExternal BIT
    DECLARE NewUser CURSOR FOR SELECT ID, LoginName, Disabled FROM inserted;
	
	OPEN NewUser  
	FETCH NEXT FROM NewUser INTO @UserID, @NewLoginName, @NewDisabled;
	
	WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @IsExternal = (SELECT IsExternal FROM Users WHERE ID = @UserID) --признак внешнего
			IF @IsExternal = 0
				BEGIN
				IF EXISTS(SELECT ID FROM deleted WHERE ID = @UserID) --было обновление строки
					BEGIN
						SET @CurrentLoginName = (SELECT LoginName FROM deleted WHERE ID = @UserID) --измененный логин
						IF (LOWER(@NewLoginName) <> LOWER(@CurrentLoginName)) --изменение логина
							BEGIN
								EXEC sp_droprolemember 'RTUsers', @CurrentLoginName
								SET @ExecString = 'DROP LOGIN ' + QUOTENAME(@CurrentLoginName) --удалить текущий логин
								EXEC (@ExecString)
								SET @ExecString = 'DROP USER ' + QUOTENAME(@CurrentLoginName) --удалить текущего пользователя
								EXEC (@ExecString)
							END
					END
				--новая строка
				IF SUSER_ID (@NewLoginName) IS NULL --такого логина нет
					BEGIN
						-- создаем логин
						IF PATINDEX('%\%', @NewLoginName) > 0 -- Windows аутентификация
							BEGIN
								SET @ExecString  = 'CREATE LOGIN '+ QUOTENAME(@NewLoginName) + ' FROM WINDOWS ' + ' WITH DEFAULT_DATABASE = AdvertisementNF' --добавить новое имя входа
								EXEC (@ExecString)
							END
						ELSE --SQL аутентификация
							BEGIN
								--SET @ExecString  = 'CREATE LOGIN '+ QUOTENAME(@NewLoginName) + ' WITH PASSWORD = ' + QUOTENAME(@Password, '''') + ', DEFAULT_DATABASE = AdvertisementNF' --добавить новое имя входа
								SET @ExecString  = 'CREATE LOGIN '+ QUOTENAME(@NewLoginName) + ' WITH PASSWORD = ' + QUOTENAME(REVERSE(@NewLoginName), '''') + ', CHECK_POLICY=OFF, DEFAULT_DATABASE = AdvertisementNF' --добавить новое имя входа
								EXEC (@ExecString)
							END
						-- создаем пользователя
						IF (@@ERROR = 0) --успешно
							BEGIN
								IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @NewLoginName) --такого пользователя нет
									BEGIN
										SET @ExecString  = 'CREATE USER ' + QUOTENAME(@NewLoginName) + ' FOR LOGIN ' + QUOTENAME(@NewLoginName) --добавить пользователя к базе
										EXEC (@ExecString)
										EXEC sp_addrolemember 'RTUsers', @NewLoginName; -- добавить к роли
									END
							END
					END
				IF SUSER_ID (@NewLoginName) IS NOT NULL --такой логин есть
					BEGIN
						IF @NewDisabled = 1
							BEGIN
								SET @ExecString = 'ALTER LOGIN ' + QUOTENAME(@NewLoginName) + ' DISABLE'
								EXEC (@ExecString)
							END
						ELSE
							BEGIN
								SET @ExecString = 'ALTER LOGIN ' + QUOTENAME(@NewLoginName) + ' ENABLE'
								EXEC (@ExecString)
							END
					END
				END
			FETCH NEXT FROM NewUser INTO @UserID, @NewLoginName, @NewDisabled;
		END
	CLOSE NewUser;  
	DEALLOCATE NewUser; 
END
GO

ALTER TABLE [dbo].[Users] ENABLE TRIGGER [OnInsertUpdateUsers]
GO

CREATE TRIGGER [dbo].[OnDeleteUsers] ON [dbo].[Users] AFTER DELETE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @ExecString VARCHAR(1000), @UserID BIGINT, @LoginName VARCHAR(200)
    DECLARE DeleteUser CURSOR FOR SELECT ID, LoginName FROM deleted;
	
	OPEN DeleteUser  
	FETCH NEXT FROM DeleteUser INTO @UserID, @LoginName;
	
	WHILE @@FETCH_STATUS = 0
		BEGIN
			IF NOT EXISTS(SELECT ID FROM Users WHERE ID = @UserID) --было удаление строки
				BEGIN
					SET @LoginName = (SELECT LoginName FROM deleted WHERE ID = @UserID) --удаленный логин
					IF SUSER_ID (@LoginName) IS NOT NULL --такой логин есть
						BEGIN
							EXEC sp_droprolemember 'RTUsers', @LoginName
							SET @ExecString = 'DROP LOGIN ' + QUOTENAME(@LoginName) --удалить логин
							EXEC (@ExecString)
							SET @ExecString = 'DROP USER ' + QUOTENAME(@LoginName) --удалить пользователя
							EXEC (@ExecString)
						END
				END
			FETCH NEXT FROM DeleteUser INTO @UserID, @LoginName;
		END
	CLOSE DeleteUser;  
	DEALLOCATE DeleteUser; 
END
GO

ALTER TABLE [dbo].[Users] ENABLE TRIGGER [OnDeleteUsers]
GO

CREATE TABLE [dbo].[Localities](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_Localities] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Banks](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[LocalitiesID] [bigint] NOT NULL,
	[CorrAccount] [varchar](20) NOT NULL,
	[BIK] [char](10) NOT NULL,
	[OKPO] [varchar](20) NULL,
	[OKONX] [varchar](20) NULL,
 CONSTRAINT [PK_Banks] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Banks]  WITH CHECK ADD  CONSTRAINT [FK_Banks_Localities] FOREIGN KEY([LocalitiesID])
REFERENCES [dbo].[Localities] ([ID])
GO

ALTER TABLE [dbo].[Banks] CHECK CONSTRAINT [FK_Banks_Localities]
GO

CREATE TABLE [dbo].[TypeOfActivitys](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[Code] [smallint] NOT NULL,
 CONSTRAINT [PK_TypeOfActivity] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Units](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_Units] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Clients](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NOT NULL,
	[Profile] [varchar](200) NULL,
	[IsIndividual] [bit] NOT NULL,
	[DirectorName] [varchar](100) NULL,
	[ContactPersonName] [varchar](100) NULL,
	[PostalAddress] [varchar](100) NULL,
	[BusinessAddress] [varchar](100) NULL,
	[Consignee] [varchar](100) NULL,
	[INN] [varchar](12) NOT NULL,
	[KPP] [varchar](9) NULL,
	[BankAccount] [varchar](20) NULL,
	[BankID] [bigint] NULL,
	[IsActive] [bit] NOT NULL,
	[Note] [varbinary](500) NULL,
	[UserID] [bigint] NOT NULL,
	[MobilePhone] [varchar](12) NULL,
	[WorkPhone] [varchar](50) NULL,
	[Email] [varchar](50) NULL,
 CONSTRAINT [PK_Clients] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Clients] ADD  CONSTRAINT [DF_Clients_PhysicalFace]  DEFAULT ((0)) FOR [IsIndividual]
GO

ALTER TABLE [dbo].[Clients] ADD  CONSTRAINT [DF_Clients_INN]  DEFAULT ('') FOR [INN]
GO

ALTER TABLE [dbo].[Clients] ADD  CONSTRAINT [DF_Clients_IsActive]  DEFAULT ((0)) FOR [IsActive]
GO

ALTER TABLE [dbo].[Clients]  WITH CHECK ADD  CONSTRAINT [FK_Clients_Banks] FOREIGN KEY([BankID])
REFERENCES [dbo].[Banks] ([ID])
GO

ALTER TABLE [dbo].[Clients] CHECK CONSTRAINT [FK_Clients_Banks]
GO

ALTER TABLE [dbo].[Clients]  WITH CHECK ADD  CONSTRAINT [FK_Clients_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO

ALTER TABLE [dbo].[Clients] CHECK CONSTRAINT [FK_Clients_Users]
GO

CREATE TABLE [dbo].[Contractors](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NOT NULL,
	[DirectorName] [varchar](100) NULL,
	[ChiefAccountant] [varchar](100) NULL,
	[BusinessAddress] [varchar](100) NULL,
	[INN] [varchar](12) NOT NULL,
	[KPP] [varchar](9) NULL,
	[OKPO] [varchar](14) NULL,
	[OGRN] [varchar](15) NULL,
	[BankAccount] [varchar](20) NULL,
	[BankID] [bigint] NULL,
 CONSTRAINT [PK_Contractors] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Contractors]  WITH CHECK ADD  CONSTRAINT [FK_Contractors_Banks] FOREIGN KEY([BankID])
REFERENCES [dbo].[Banks] ([ID])
GO

ALTER TABLE [dbo].[Contractors] CHECK CONSTRAINT [FK_Contractors_Banks]
GO



