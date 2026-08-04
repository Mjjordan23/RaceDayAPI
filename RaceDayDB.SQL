USE master;
GO

DROP DATABASE IF EXISTS RaceDayDB;
GO

-- ==========================================
-- RaceDayDB
-- Database Creation Script
-- ==========================================

-- Create Database
CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ==========================================
-- Users Table
-- ==========================================

CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL
        CHECK (Role IN ('Organiser','Participant')),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- ==========================================
-- Participants Table
-- ==========================================

CREATE TABLE Participants
(
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    DateOfBirth DATE NOT NULL,
    PhoneNumber VARCHAR(20),
    ProfilePictureUrl VARCHAR(255),

    CONSTRAINT FK_Participants_Users
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
);

-- ==========================================
-- Organisers Table
-- ==========================================

CREATE TABLE Organisers
(
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    OrganisationName VARCHAR(100) NOT NULL,
    ContactNumber VARCHAR(20),

    CONSTRAINT FK_Organisers_Users
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
);

-- ==========================================
-- Events Table
-- ==========================================

CREATE TABLE Events
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(MAX) NOT NULL,
    EventDate DATETIME NOT NULL,
    Location VARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    EventType VARCHAR(20) NOT NULL
        CHECK (EventType IN ('Run','Walk','Cycle')),
    BannerImageUrl VARCHAR(255),

    CONSTRAINT FK_Events_Organisers
        FOREIGN KEY (OrganiserID)
        REFERENCES Organisers(OrganiserID)
);

-- ==========================================
-- Categories Table
-- ==========================================

CREATE TABLE Categories
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name VARCHAR(50) NOT NULL,
    MinAge INT,
    MaxAge INT,
    DistanceKm DECIMAL(5,2),

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);

-- ==========================================
-- Enrolments Table
-- ==========================================

CREATE TABLE Enrolments
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    Status VARCHAR(20) NOT NULL
        CHECK (Status IN ('Pending','Confirmed')),
    EnrolledAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Enrolments_Participants
        FOREIGN KEY (ParticipantID)
        REFERENCES Participants(ParticipantID),

    CONSTRAINT FK_Enrolments_Events
        FOREIGN KEY (EventID)
        REFERENCES Events(EventID),

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),

    CONSTRAINT UQ_Enrolments UNIQUE (ParticipantID, EventID)
);

-- ==========================================
-- Results Table
-- ==========================================

CREATE TABLE Results
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME,
    FinishPosition INT,
    RecordedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID)
);

-- ==========================================
-- INSERT SAMPLE DATA
-- ==========================================

-- Users
INSERT INTO Users (Email, PasswordHash, Role)
VALUES
('john.organiser@gmail.com', 'Password123', 'Organiser'),
('mary.organiser@gmail.com', 'Password123', 'Organiser'),
('thabo.participant@gmail.com', 'Password123', 'Participant'),
('linda.participant@gmail.com', 'Password123', 'Participant');

-- Organisers
INSERT INTO Organisers (UserID, OrganisationName, ContactNumber)
VALUES
(1, 'Race Events SA', '0821234567'),
(2, 'Marathon Masters', '0839876543');

-- Participants
INSERT INTO Participants (UserID, FirstName, LastName, DateOfBirth, PhoneNumber, ProfilePictureUrl)
VALUES
(3, 'Thabo', 'Mokoena', '2001-05-14', '0712345678', NULL),
(4, 'Linda', 'Nkosi', '1999-10-30', '0723456789', NULL);

-- Events
INSERT INTO Events
(OrganiserID, Name, Description, EventDate, Location, DistanceKm, EventType, BannerImageUrl)
VALUES
(1, 'Johannesburg City Marathon', 'Annual city marathon',
'2026-09-20 07:00:00', 'Johannesburg', 42.20, 'Run', NULL),

(2, 'Cape Town Fun Walk', 'Family fun walk',
'2026-10-15 08:00:00', 'Cape Town', 10.00, 'Walk', NULL),

(1, 'Durban Cycle Challenge', 'Cycling competition',
'2026-11-05 06:30:00', 'Durban', 50.00, 'Cycle', NULL);

-- Categories
INSERT INTO Categories
(EventID, Name, MinAge, MaxAge, DistanceKm)
VALUES
(1, 'Senior', 18, 39, 42.20),
(1, 'Veteran', 40, 60, 42.20),
(2, 'Fun Walk', 10, NULL, 10.00),
(3, 'Open', 18, NULL, 50.00);

-- Enrolments
INSERT INTO Enrolments
(ParticipantID, EventID, CategoryID, Status)
VALUES
(1, 1, 1, 'Confirmed'),
(2, 2, 3, 'Confirmed'),
(1, 3, 4, 'Pending');

-- Results
INSERT INTO Results
(EnrolmentID, FinishTime, FinishPosition)
VALUES
(1, '03:45:30', 12),
(2, '01:40:15', 5);

-- ==========================================
-- DISPLAY DATA
-- ==========================================

SELECT * FROM Users;
SELECT * FROM Organisers;
SELECT * FROM Participants;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;