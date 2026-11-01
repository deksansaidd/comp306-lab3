-- =====================================================================
-- COMP306 Lab 3 - Learning Resource Portal
-- =====================================================================

-- Delete the LearningPortal database if it exists.
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'LearningPortal')
BEGIN
    DROP DATABASE LearningPortal;
END
GO

CREATE DATABASE LearningPortal;
GO

USE LearningPortal;
GO

-- ---------------------------------------------------------------------
-- Users
-- ---------------------------------------------------------------------
CREATE TABLE dbo.Users (
    UserID       INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    Username     VARCHAR(50)  NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    Email        VARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash VARCHAR(64)  NOT NULL,
    Role         VARCHAR(20)  NOT NULL
                 CONSTRAINT CK_Users_Role CHECK (Role IN ('Instructor', 'Student')),
    CreatedDate  DATETIME2    NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT SYSUTCDATETIME()
);
GO

DECLARE @pw VARCHAR(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Password1!'), 2));

INSERT INTO dbo.Users (Username, Email, PasswordHash, Role) VALUES
    ('jsmith',   'jsmith@college.ca',   @pw, 'Instructor'),   -- UserID 1
    ('mlee',     'mlee@college.ca',     @pw, 'Instructor'),   -- UserID 2
    ('pnguyen',  'pnguyen@college.ca',  @pw, 'Instructor'),   -- UserID 3
    ('aomar',    'aomar@college.ca',    @pw, 'Student'),      -- UserID 4
    ('kbrown',   'kbrown@college.ca',   @pw, 'Student'),      -- UserID 5
    ('tsingh',   'tsingh@college.ca',   @pw, 'Student');      -- UserID 6
GO

-- ---------------------------------------------------------------------
-- Categories
-- ---------------------------------------------------------------------
CREATE TABLE dbo.Categories (
    CategoryID   INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    CategoryName VARCHAR(50) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE
);
GO

INSERT INTO dbo.Categories (CategoryName) VALUES
    ('Programming'),        -- CategoryID 1
    ('Databases'),          -- CategoryID 2
    ('Cloud Computing'),    -- CategoryID 3
    ('Web Development'),    -- CategoryID 4
    ('Software Testing'),   -- CategoryID 5
    ('Networking');         -- CategoryID 6
GO

-- ---------------------------------------------------------------------
-- LearningResources
-- S3Key is the object key of the uploaded file in the S3 bucket
-- (the file itself is NOT stored in the database).
-- ---------------------------------------------------------------------
CREATE TABLE dbo.LearningResources (
    ResourceID   INT IDENTITY(1,1) CONSTRAINT PK_LearningResources PRIMARY KEY,
    Title        VARCHAR(150)  NOT NULL,
    Description  VARCHAR(1000) NOT NULL,
    CategoryID   INT           NOT NULL,
    AuthorID     INT           NOT NULL,
    CreatedDate  DATETIME2     NOT NULL CONSTRAINT DF_Resources_CreatedDate DEFAULT SYSUTCDATETIME(),
    S3Key        VARCHAR(300)  NOT NULL,
    ViewCount    INT           NOT NULL CONSTRAINT DF_Resources_ViewCount DEFAULT 0,
    CONSTRAINT CK_Resources_ViewCount CHECK (ViewCount >= 0),
    CONSTRAINT FK_Resources_Category FOREIGN KEY (CategoryID) REFERENCES dbo.Categories (CategoryID),
    CONSTRAINT FK_Resources_Author   FOREIGN KEY (AuthorID)   REFERENCES dbo.Users (UserID)
);
GO

-- Helps "newest first" and "most popular" listings.
CREATE INDEX IX_Resources_CreatedDate ON dbo.LearningResources (CreatedDate DESC);
CREATE INDEX IX_Resources_ViewCount   ON dbo.LearningResources (ViewCount DESC);
GO

INSERT INTO dbo.LearningResources (Title, Description, CategoryID, AuthorID, CreatedDate, S3Key, ViewCount) VALUES
    ('Introduction to SQL Joins',
     'A practical guide to INNER, LEFT and RIGHT joins with worked examples.',
     2, 1, DATEADD(DAY, -30, SYSUTCDATETIME()), 'resources/sql-joins.pdf', 142),

    ('C# Fundamentals Cheat Sheet',
     'Quick reference for C# syntax, collections, LINQ and common patterns.',
     1, 2, DATEADD(DAY, -25, SYSUTCDATETIME()), 'resources/csharp-cheatsheet.pdf', 98),

    ('Getting Started with AWS Lambda',
     'How serverless functions work, how they are triggered, and how to deploy one.',
     3, 3, DATEADD(DAY, -21, SYSUTCDATETIME()), 'resources/aws-lambda-intro.pdf', 215),

    ('HTML and CSS Layout Basics',
     'Flexbox, grid and responsive layout fundamentals for web pages.',
     4, 2, DATEADD(DAY, -18, SYSUTCDATETIME()), 'resources/html-css-layout.pdf', 76),

    ('Unit Testing with xUnit',
     'Writing, organizing and running unit tests in .NET using xUnit.',
     5, 1, DATEADD(DAY, -14, SYSUTCDATETIME()), 'resources/unit-testing-xunit.pdf', 64),

    ('Designing REST APIs',
     'Resource naming, HTTP verbs, status codes and versioning for REST services.',
     4, 3, DATEADD(DAY, -10, SYSUTCDATETIME()), 'resources/rest-api-design.pdf', 187),

    ('Database Normalization Explained',
     'First, second and third normal form with before-and-after examples.',
     2, 1, DATEADD(DAY, -6, SYSUTCDATETIME()), 'resources/normalization.pdf', 120),

    ('Docker for Beginners',
     'Images, containers, volumes and a first multi-container application.',
     3, 2, DATEADD(DAY, -2, SYSUTCDATETIME()), 'resources/docker-beginners.pdf', 53);
GO

-- ---------------------------------------------------------------------
-- Quick check: every table should show at least 5 rows.
-- ---------------------------------------------------------------------
SELECT 'Users' AS TableName, COUNT(*) AS RowsInTable FROM dbo.Users
UNION ALL SELECT 'Categories', COUNT(*) FROM dbo.Categories
UNION ALL SELECT 'LearningResources', COUNT(*) FROM dbo.LearningResources;
GO