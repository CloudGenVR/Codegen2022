CREATE TABLE [dbo].[Comments](
	[Id] [uniqueidentifier] NOT NULL,
	[PhotoId] [uniqueidentifier] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Text] [nvarchar](max) NOT NULL,
	[SentimentScore] [float] NOT NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))
GO

CREATE TABLE [dbo].[Photos](
	[Id] [uniqueidentifier] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Path] [varchar](512) NOT NULL,
	[Name] [varchar](256) NOT NULL,
	[Size] [bigint] NOT NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_Photos] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))
GO

ALTER TABLE [dbo].[Comments] ADD  CONSTRAINT [DF_Comments_Id]  DEFAULT (newid()) FOR [Id]
GO

ALTER TABLE [dbo].[Photos] ADD  CONSTRAINT [DF_Photos_Id]  DEFAULT (newid()) FOR [Id]
GO

ALTER TABLE [dbo].[Comments]  WITH CHECK ADD  CONSTRAINT [FK_Comments_Photos] FOREIGN KEY([PhotoId])
REFERENCES [dbo].[Photos] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Comments] CHECK CONSTRAINT [FK_Comments_Photos]
GO
