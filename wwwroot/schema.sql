PRAGMA foreign_keys = ON;

drop table if EXISTS `User`;
CREATE TABLE `User` (
    `UserID` integer NOT NULL PRIMARY KEY,
    `Email` text not NULL,
    `PasswordHash` text not NULL,
    `PasswordSalt` text not NULL
);

drop table if EXISTS `Session`;
CREATE TABLE `Session` (
    `SessionID` integer NOT NULL PRIMARY KEY,
    `SessionCookie` text not null UNIQUE,
    `UserID` integer not null,
    `ValidUntil` integer not null,
    `LoginTime` integer not null,
    FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`)
);

drop table if EXISTS `Entry`;
CREATE TABLE `Entry` (
    `EntryID` integer NOT NULL PRIMARY KEY,
    `Title` text,
    `Content` text,
    `AuthorID` integer NOT NULL,
    `CreationTime` integer NOT NULL,
    FOREIGN KEY (`AuthorID`) REFERENCES `User`(`UserID`)
);