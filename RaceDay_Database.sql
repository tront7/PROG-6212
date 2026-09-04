/* ============================================================
   RaceDay Database Script (Section C)
   This script matches the ERD in Section A exactly:
   Roles, Users, Events, Categories, Enrolments, Results.
   Tested to run without errors on a clean SQL Server instance.
   ============================================================ */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ============================================================
   TABLE CREATION
   ============================================================ */

CREATE TABLE Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    VARCHAR(20) NOT NULL UNIQUE
);
GO

CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    PhoneNumber     VARCHAR(20) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
);
GO

CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       VARCHAR(150) NOT NULL,
    EventDate       DATETIME NOT NULL,
    Location        VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES Users(UserId)
);
GO

CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    VARCHAR(50) NOT NULL,
    DistanceKm      DECIMAL(6,2) NOT NULL,
    MaxParticipants INT NOT NULL,
    Price           DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId) ON DELETE CASCADE
);
GO

CREATE TABLE Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL,
    EventId         INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_UserCategory UNIQUE (UserId, CategoryId)
);
GO

CREATE TABLE Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          VARCHAR(20) NOT NULL DEFAULT 'Finished',
    RecordedAt      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES Enrolments(EnrolmentId)
);
GO

/* ============================================================
   SAMPLE DATA
   ============================================================ */

INSERT INTO Roles (RoleName) VALUES
('Organiser'),
('Participant');
GO

INSERT INTO Users (RoleId, FullName, Email, PasswordHash, PhoneNumber) VALUES
(1, 'Thabo Nkosi',    'thabo.nkosi@raceday.co.za',    'hashed_pw_1', '0821234567'),
(1, 'Lindiwe Dube',   'lindiwe.dube@raceday.co.za',   'hashed_pw_2', '0837654321'),
(2, 'Sipho Mahlangu', 'sipho.mahlangu@example.com',   'hashed_pw_3', '0731112223'),
(2, 'Anje van Wyk',   'anje.vanwyk@example.com',      'hashed_pw_4', '0824445556'),
(2, 'Priya Naidoo',   'priya.naidoo@example.com',     'hashed_pw_5', '0719998887');
GO

INSERT INTO Events (OrganiserId, EventName, EventDate, Location, Description) VALUES
(1, 'Johannesburg City Marathon', '2026-10-04 06:00:00', 'Johannesburg, Gauteng', 'Annual road marathon through the city centre.'),
(2, 'Durban Beachfront Fun Run',  '2026-09-20 07:00:00', 'Durban, KwaZulu-Natal', 'Family-friendly fun run along the beachfront.');
GO

INSERT INTO Categories (EventId, CategoryName, DistanceKm, MaxParticipants, Price) VALUES
(1, '5km Fun Run', 5.0, 500, 100.00),
(1, '10km Race',   10.0, 800, 150.00),
(1, '21km Half Marathon', 21.1, 1000, 250.00),
(2, '5km Fun Run', 5.0, 300, 80.00),
(2, '10km Race',   10.0, 400, 120.00);
GO

INSERT INTO Enrolments (UserId, EventId, CategoryId, Status) VALUES
(3, 1, 2, 'Confirmed'),
(4, 1, 3, 'Confirmed'),
(5, 2, 4, 'Confirmed');
GO

INSERT INTO Results (EnrolmentId, FinishTime, Position, Status) VALUES
(1, '00:52:30', 15, 'Finished'),
(2, '01:48:12', 42, 'Finished'),
(3, '00:24:05', 3,  'Finished');
GO

/* ============================================================
   VERIFICATION QUERIES
   ============================================================ */

SELECT * FROM Roles;
SELECT * FROM Users;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;
GO
