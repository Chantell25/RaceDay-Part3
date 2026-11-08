/* RaceDay - Part 1 SQL Server Database Script
   Module: PROG6212 Programming 2B
   Purpose: Create and seed the complete RaceDay database planned in the ERD.
*/

IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END;
GO

USE RaceDayDB;
GO

/* Make the script safely re-runnable on a development database. */
IF OBJECT_ID(N'dbo.Result', N'U') IS NOT NULL DROP TABLE dbo.Result;
IF OBJECT_ID(N'dbo.Enrollment', N'U') IS NOT NULL DROP TABLE dbo.Enrollment;
IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID(N'dbo.Event', N'U') IS NOT NULL DROP TABLE dbo.Event;
IF OBJECT_ID(N'dbo.ParticipantProfile', N'U') IS NOT NULL DROP TABLE dbo.ParticipantProfile;
IF OBJECT_ID(N'dbo.OrganiserProfile', N'U') IS NOT NULL DROP TABLE dbo.OrganiserProfile;
IF OBJECT_ID(N'dbo.UserAccount', N'U') IS NOT NULL DROP TABLE dbo.UserAccount;
GO

CREATE TABLE dbo.UserAccount
(
    UserId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_UserAccount PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL
        CONSTRAINT UQ_UserAccount_Email UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL
        CONSTRAINT CK_UserAccount_Role CHECK (Role IN (N'Organiser', N'Participant')),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_UserAccount_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL
        CONSTRAINT DF_UserAccount_IsActive DEFAULT 1
);
GO

CREATE TABLE dbo.OrganiserProfile
(
    OrganiserId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_OrganiserProfile PRIMARY KEY,
    UserId INT NOT NULL
        CONSTRAINT UQ_OrganiserProfile_UserId UNIQUE,
    OrganisationName NVARCHAR(120) NOT NULL,
    ContactNumber NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_OrganiserProfile_UserAccount
        FOREIGN KEY (UserId) REFERENCES dbo.UserAccount(UserId)
);
GO

CREATE TABLE dbo.ParticipantProfile
(
    ParticipantId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_ParticipantProfile PRIMARY KEY,
    UserId INT NOT NULL
        CONSTRAINT UQ_ParticipantProfile_UserId UNIQUE,
    DateOfBirth DATE NULL,
    EmergencyContactName NVARCHAR(100) NULL,
    EmergencyContactNumber NVARCHAR(30) NULL,
    ProfilePictureUrl NVARCHAR(1000) NULL,
    CONSTRAINT FK_ParticipantProfile_UserAccount
        FOREIGN KEY (UserId) REFERENCES dbo.UserAccount(UserId)
);
GO

CREATE TABLE dbo.Event
(
    EventId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Event PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventDate DATE NOT NULL,
    Venue NVARCHAR(150) NOT NULL,
    City NVARCHAR(80) NOT NULL,
    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Event_Status DEFAULT N'Planned',
    BannerUrl NVARCHAR(1000) NULL,
    EventType NVARCHAR(50) NOT NULL CONSTRAINT DF_Event_EventType DEFAULT N'Road Race',
    CONSTRAINT CK_Event_Status CHECK (Status IN (N'Planned', N'Open', N'Closed', N'Completed', N'Cancelled')),
    CONSTRAINT FK_Event_OrganiserProfile
        FOREIGN KEY (OrganiserId) REFERENCES dbo.OrganiserProfile(OrganiserId)
);
GO

CREATE TABLE dbo.Category
(
    CategoryId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Category PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    MaxParticipants INT NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    CONSTRAINT UQ_Category_Event_CategoryName UNIQUE (EventId, CategoryName),
    CONSTRAINT UQ_Category_Event_CategoryId UNIQUE (EventId, CategoryId),
    CONSTRAINT CK_Category_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Category_MaxParticipants CHECK (MaxParticipants > 0),
    CONSTRAINT CK_Category_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT FK_Category_Event
        FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.Enrollment
(
    EnrollmentId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Enrollment PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    ParticipantId INT NOT NULL,
    EnrollmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrollment_EnrollmentDate DEFAULT SYSUTCDATETIME(),
    RaceNumber INT NOT NULL,
    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrollment_Status DEFAULT N'Confirmed',
    CONSTRAINT UQ_Enrollment_RaceNumber UNIQUE (RaceNumber),
    CONSTRAINT UQ_Enrollment_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT CK_Enrollment_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled', N'Completed')),
    CONSTRAINT FK_Enrollment_ParticipantProfile
        FOREIGN KEY (ParticipantId) REFERENCES dbo.ParticipantProfile(ParticipantId),
    CONSTRAINT FK_Enrollment_CategoryEvent
        FOREIGN KEY (EventId, CategoryId) REFERENCES dbo.Category(EventId, CategoryId)
);
GO

CREATE TABLE dbo.Result
(
    ResultId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Result PRIMARY KEY,
    EnrollmentId INT NOT NULL
        CONSTRAINT UQ_Result_EnrollmentId UNIQUE,
    FinishTime TIME(0) NULL,
    Position INT NULL,
    ResultStatus NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Result_ResultStatus DEFAULT N'Pending',
    RecordedAt DATETIME2(0) NULL,
    CONSTRAINT CK_Result_Position CHECK (Position IS NULL OR Position > 0),
    CONSTRAINT CK_Result_Status CHECK (ResultStatus IN (N'Pending', N'Finished', N'DNF', N'DNS')),
    CONSTRAINT FK_Result_Enrollment
        FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollment(EnrollmentId)
);
GO

/* -----------------------------
   Seed data: Users and profiles
   Demo logins: organisers use Organiser@123, participants use Participant@123
   ----------------------------- */

INSERT INTO dbo.UserAccount
    (FirstName, LastName, Email, PasswordHash, Role)
VALUES
    (N'Lerato', N'Mokoena', N'lerato.organiser@raceday.example', N'PBKDF2-SHA256.120000.MAsbxpTGeIY2W4IIbrbnaw==.0Yv4l3M1DDcF5d7tRNbUujem/rGugRMVJVf3GyT6Z9c=', N'Organiser'),
    (N'Jason', N'Naidoo', N'jason.organiser@raceday.example', N'PBKDF2-SHA256.120000.Y5hhj4AfsJ2hwr6jdvsr0g==.m4Oyu6vkC5fBIv2k6P9Z927VkCqoBeQC5Ra4aq77K/0=', N'Organiser'),
    (N'Ayanda', N'Dlamini', N'ayanda.participant@raceday.example', N'PBKDF2-SHA256.120000.GbY2pthuvoTAz9AzFjnNNQ==.ZS5kPaiTwuGaH7lzE68bp7w36vEEgbqR1muVjvo6wPg=', N'Participant'),
    (N'Kabelo', N'Molefe', N'kabelo.participant@raceday.example', N'PBKDF2-SHA256.120000.vWAwLDgQYxlGP1sOQo4vdQ==.yZpLcIVYdbrXp8iaEix1fyA4HhhLoV3kCUv98g4JJAU=', N'Participant'),
    (N'Naledi', N'Pillay', N'naledi.participant@raceday.example', N'PBKDF2-SHA256.120000.EoAntLL3xSWg213x1mAo/Q==.Nu80SgU2ajuSAxJ9UNZkqUPczbP24JTtNptAmka56AY=', N'Participant'),
    (N'Tumi', N'Jacobs', N'tumi.participant@raceday.example', N'PBKDF2-SHA256.120000.P4jH949e1Ce/+LeUr2oxug==.lqGZy6vrOlis6ON8TkyKIFQNXIM8EDtYCj5NHVJWBtA=', N'Participant');
GO

INSERT INTO dbo.OrganiserProfile
    (UserId, OrganisationName, ContactNumber)
SELECT UserId, N'RaceDay Gauteng Events', N'011-555-0101'
FROM dbo.UserAccount
WHERE Email = N'lerato.organiser@raceday.example';

INSERT INTO dbo.OrganiserProfile
    (UserId, OrganisationName, ContactNumber)
SELECT UserId, N'Coastal Active SA', N'021-555-0102'
FROM dbo.UserAccount
WHERE Email = N'jason.organiser@raceday.example';
GO

INSERT INTO dbo.ParticipantProfile
    (UserId, DateOfBirth, EmergencyContactName, EmergencyContactNumber)
SELECT UserId, '1999-04-15', N'Mpho Dlamini', N'082-555-1001'
FROM dbo.UserAccount
WHERE Email = N'ayanda.participant@raceday.example';

INSERT INTO dbo.ParticipantProfile
    (UserId, DateOfBirth, EmergencyContactName, EmergencyContactNumber)
SELECT UserId, '1997-09-22', N'Palesa Molefe', N'083-555-1002'
FROM dbo.UserAccount
WHERE Email = N'kabelo.participant@raceday.example';

INSERT INTO dbo.ParticipantProfile
    (UserId, DateOfBirth, EmergencyContactName, EmergencyContactNumber)
SELECT UserId, '2001-02-11', N'Sipho Pillay', N'084-555-1003'
FROM dbo.UserAccount
WHERE Email = N'naledi.participant@raceday.example';

INSERT INTO dbo.ParticipantProfile
    (UserId, DateOfBirth, EmergencyContactName, EmergencyContactNumber)
SELECT UserId, '1995-11-03', N'Refilwe Jacobs', N'085-555-1004'
FROM dbo.UserAccount
WHERE Email = N'tumi.participant@raceday.example';
GO

/* -----------------------------
   Seed data: three events
   ----------------------------- */

INSERT INTO dbo.Event
    (OrganiserId, EventName, Description, EventDate, Venue, City, Status)
SELECT OrganiserId,
       N'Johannesburg Spring Run',
       N'Road-running event with multiple distance categories.',
       CAST(DATEADD(DAY, -21, GETDATE()) AS DATE),
       N'Zoo Lake Sports Grounds',
       N'Johannesburg',
       N'Completed'
FROM dbo.OrganiserProfile
WHERE OrganisationName = N'RaceDay Gauteng Events';

INSERT INTO dbo.Event
    (OrganiserId, EventName, Description, EventDate, Venue, City, Status)
SELECT OrganiserId,
       N'Cape Town Cycle Challenge',
       N'Community cycling event with road-cycling categories.',
       CAST(DATEADD(DAY, 14, GETDATE()) AS DATE),
       N'Green Point Urban Park',
       N'Cape Town',
       N'Open'
FROM dbo.OrganiserProfile
WHERE OrganisationName = N'Coastal Active SA';

INSERT INTO dbo.Event
    (OrganiserId, EventName, Description, EventDate, Venue, City, Status)
SELECT OrganiserId,
       N'Durban Beach Run and Walk',
       N'Beachfront running and walking event for the community.',
       CAST(DATEADD(DAY, 30, GETDATE()) AS DATE),
       N'Moses Mabhida Precinct',
       N'Durban',
       N'Open'
FROM dbo.OrganiserProfile
WHERE OrganisationName = N'Coastal Active SA';
GO

INSERT INTO dbo.Event
    (OrganiserId, EventName, Description, EventDate, Venue, City, Status, EventType)
SELECT o.OrganiserId, v.EventName, v.Description, CAST(DATEADD(DAY, v.DaysAhead, GETDATE()) AS DATE), v.Venue, v.City, v.Status, v.EventType
FROM (VALUES
    (N'Coastal Active SA',        N'Table Mountain Trail Run',  N'Technical trail running on the slopes of Table Mountain with ocean views.', 45, N'Kloof Nek Car Park',   N'Cape Town',   N'Open',    N'Trail Run'),
    (N'RaceDay Gauteng Events',   N'Pretoria Cycle Classic',    N'Closed-road cycling through the jacaranda-lined streets of Pretoria.',     60, N'Union Buildings',      N'Pretoria',    N'Open',    N'Cycling'),
    (N'RaceDay Gauteng Events',   N'Joburg City 10K',           N'A fast, flat city route with a lively finish at Constitution Hill.',       75, N'Constitution Hill',    N'Johannesburg',N'Open',    N'Road Race'),
    (N'Coastal Active SA',        N'Winelands Half Marathon',   N'Run through the vineyards of Stellenbosch on quiet country roads.',        90, N'Jonkershoek Valley',   N'Stellenbosch',N'Planned', N'Road Race')
) AS v(OrganisationName, EventName, Description, DaysAhead, Venue, City, Status, EventType)
JOIN dbo.OrganiserProfile o ON o.OrganisationName = v.OrganisationName;
GO

UPDATE dbo.Event SET EventType = N'Cycling'  WHERE EventName = N'Cape Town Cycle Challenge';
UPDATE dbo.Event SET EventType = N'Fun Run'   WHERE EventName = N'Durban Beach Run and Walk';
GO

/* Categories for every event */
INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'5 KM Run', 5.00, 500, 120.00
FROM dbo.Event WHERE EventName = N'Johannesburg Spring Run';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'10 KM Run', 10.00, 750, 180.00
FROM dbo.Event WHERE EventName = N'Johannesburg Spring Run';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'21.1 KM Half Marathon', 21.10, 1000, 280.00
FROM dbo.Event WHERE EventName = N'Johannesburg Spring Run';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'20 KM Cycle', 20.00, 500, 220.00
FROM dbo.Event WHERE EventName = N'Cape Town Cycle Challenge';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'40 KM Cycle', 40.00, 750, 320.00
FROM dbo.Event WHERE EventName = N'Cape Town Cycle Challenge';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'5 KM Beach Walk', 5.00, 600, 100.00
FROM dbo.Event WHERE EventName = N'Durban Beach Run and Walk';

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT EventId, N'10 KM Beach Run', 10.00, 800, 160.00
FROM dbo.Event WHERE EventName = N'Durban Beach Run and Walk';
GO

INSERT INTO dbo.Category (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee)
SELECT e.EventId, v.CategoryName, v.DistanceKm, v.MaxParticipants, v.EntryFee
FROM (VALUES
    (N'Table Mountain Trail Run', N'12 KM Trail',            12.00, 250, 220.00),
    (N'Table Mountain Trail Run', N'25 KM Trail',            25.00, 150, 350.00),
    (N'Pretoria Cycle Classic',   N'40 KM Sprint',           40.00, 300, 250.00),
    (N'Pretoria Cycle Classic',   N'94 KM Classic',          94.00, 300, 420.00),
    (N'Joburg City 10K',          N'5 KM Fun Run',            5.00, 500, 120.00),
    (N'Joburg City 10K',          N'10 KM Open',             10.00, 800, 180.00),
    (N'Winelands Half Marathon',  N'10 KM Open',             10.00, 400, 190.00),
    (N'Winelands Half Marathon',  N'21.1 KM Half Marathon',  21.10, 400, 300.00)
) AS v(EventName, CategoryName, DistanceKm, MaxParticipants, EntryFee)
JOIN dbo.Event e ON e.EventName = v.EventName;
GO

/* Sample enrolments: each participant is enrolled once per event. */
INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 1001, N'Completed'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'10 KM Run'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'ayanda.participant@raceday.example')
WHERE e.EventName = N'Johannesburg Spring Run';

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 1002, N'Completed'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'10 KM Run'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'kabelo.participant@raceday.example')
WHERE e.EventName = N'Johannesburg Spring Run';

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 2001, N'Confirmed'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'20 KM Cycle'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'ayanda.participant@raceday.example')
WHERE e.EventName = N'Cape Town Cycle Challenge';

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 2002, N'Confirmed'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'40 KM Cycle'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'naledi.participant@raceday.example')
WHERE e.EventName = N'Cape Town Cycle Challenge';

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 3001, N'Confirmed'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'5 KM Beach Walk'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'kabelo.participant@raceday.example')
WHERE e.EventName = N'Durban Beach Run and Walk';

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, 3002, N'Pending'
FROM dbo.Event e
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = N'10 KM Beach Run'
JOIN dbo.ParticipantProfile p ON p.UserId = (SELECT UserId FROM dbo.UserAccount WHERE Email = N'tumi.participant@raceday.example')
WHERE e.EventName = N'Durban Beach Run and Walk';
GO

INSERT INTO dbo.Enrollment
    (EventId, CategoryId, ParticipantId, RaceNumber, Status)
SELECT e.EventId, c.CategoryId, p.ParticipantId, v.RaceNumber, v.Status
FROM (VALUES
    (N'Johannesburg Spring Run', N'10 KM Run',      N'naledi.participant@raceday.example', 1003, N'Completed'),
    (N'Johannesburg Spring Run', N'10 KM Run',      N'tumi.participant@raceday.example',   1004, N'Completed'),
    (N'Table Mountain Trail Run',N'12 KM Trail',    N'ayanda.participant@raceday.example', 4001, N'Confirmed'),
    (N'Pretoria Cycle Classic',  N'40 KM Sprint',   N'kabelo.participant@raceday.example', 5001, N'Pending')
) AS v(EventName, CategoryName, Email, RaceNumber, Status)
JOIN dbo.Event e ON e.EventName = v.EventName
JOIN dbo.Category c ON c.EventId = e.EventId AND c.CategoryName = v.CategoryName
JOIN dbo.UserAccount u ON u.Email = v.Email
JOIN dbo.ParticipantProfile p ON p.UserId = u.UserId;
GO

/* Sample results linked to enrolments. */
INSERT INTO dbo.Result (EnrollmentId, FinishTime, Position, ResultStatus, RecordedAt)
SELECT EnrollmentId, v.FinishTime, v.Position, N'Finished', DATEADD(DAY, -20, SYSUTCDATETIME())
FROM (VALUES (1001, '00:41:12', 1), (1002, '00:47:30', 2), (1003, '00:52:08', 3), (1004, '00:58:45', 4))
     AS v(RaceNumber, FinishTime, Position)
JOIN dbo.Enrollment en ON en.RaceNumber = v.RaceNumber;
GO

/* Verification queries for the Part 1 demonstration. */
SELECT COUNT(*) AS UserCount FROM dbo.UserAccount;
SELECT COUNT(*) AS OrganiserCount FROM dbo.OrganiserProfile;
SELECT COUNT(*) AS ParticipantCount FROM dbo.ParticipantProfile;
SELECT COUNT(*) AS EventCount FROM dbo.Event;
SELECT COUNT(*) AS CategoryCount FROM dbo.Category;
SELECT COUNT(*) AS EnrollmentCount FROM dbo.Enrollment;
SELECT COUNT(*) AS ResultCount FROM dbo.Result;

SELECT
    e.EventName,
    c.CategoryName,
    p.ParticipantId,
    u.FirstName + N' ' + u.LastName AS ParticipantName,
    en.RaceNumber,
    en.Status AS EnrollmentStatus,
    r.ResultStatus,
    r.FinishTime,
    r.Position
FROM dbo.Enrollment en
JOIN dbo.Event e ON e.EventId = en.EventId
JOIN dbo.Category c ON c.CategoryId = en.CategoryId
JOIN dbo.ParticipantProfile p ON p.ParticipantId = en.ParticipantId
JOIN dbo.UserAccount u ON u.UserId = p.UserId
LEFT JOIN dbo.Result r ON r.EnrollmentId = en.EnrollmentId
ORDER BY e.EventDate, en.RaceNumber;
GO
