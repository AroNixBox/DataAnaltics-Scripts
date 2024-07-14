-- create our DB
CREATE DATABASE worldbuilding;
-- Select the database to use, so the following calls know what db to relate to
USE worldbuilding; 

-- Create the 'buildingtypes' table
CREATE TABLE buildingtypes (
    Name VARCHAR(50) NOT NULL,  -- Name of the building type (e.g., "House", "Tower")
    PRIMARY KEY (Name)           -- Set the 'Name' column as the primary key (unique identifier), we prefer string over integer,
                                -- because in C# when a buildingtype (enum) is deleted, the following ones all get decremented
                                -- and we would have issues in the db (eg. House is deleted, Tower is the new index 0, and 
                                -- if we load the game, every database index 1 (which was Tower before) is now out of Bounds.
);

-- Insert a dummy building type 'Tower' (for demonstration purposes)
INSERT INTO buildingtypes (Name) VALUES ('Tower'); 

-- Create the 'users' table
CREATE TABLE users (
    Email VARCHAR(50) NOT NULL PRIMARY KEY,  -- Email of the user (primary key, also used for login)
    Username VARCHAR(15),                    -- Username of the user
    Password VARCHAR(150)                    -- MD5 Hashed password of the user, Look into Argon 2 in the future, MD5 not safe anymore
);

-- Create the 'playerbuildings' table
CREATE TABLE playerbuildings (
    BuildingInstanceID INT AUTO_INCREMENT PRIMARY KEY,  -- Unique ID for each building instance, automatically incremented, to identify each building
    Email VARCHAR(50) NOT NULL,                       -- Email of the player who owns the building, so we can only load buildings from a specific player when he loggs in
    BuildingName VARCHAR(50) NOT NULL,                  -- Name of the building type (references 'buildingtypes' table)
    PosX INT NOT NULL,                                  -- X coordinate of the building's position on Grid
    PosZ INT NOT NULL,                                  -- Z coordinate of the building's position on Grid
    FOREIGN KEY (Email) REFERENCES users(Email),        -- Reference to the email of the user, who build the building
    FOREIGN KEY (BuildingName) REFERENCES buildingtypes(Name) -- Enforce referential integrity with 'buildingtypes'
);

-- Insert sample user data (for demonstration purposes)
INSERT INTO users (Email, Username, Password) VALUES
('user1@example.com', 'User1', 'password123'),
('user2@example.com', 'User2', 'securepass'),
('user3@example.com', 'User3', 'anotherpassword'),
('user4@example.com', 'User4', 'secret'),
('user5@example.com', 'User5', '12345');

-- Insert sample building data (different tower counts for each user) (for demonstration purposes)
INSERT INTO playerbuildings (Email, BuildingName, PosX, PosZ) VALUES
('user1@example.com', 'Tower', 1, 2),
('user1@example.com', 'Tower', 3, 4),
('user2@example.com', 'Tower', 5, 6),
('user3@example.com', 'Tower', 7, 8),
('user3@example.com', 'Tower', 9, 10),
('user3@example.com', 'Tower', 11, 12),
('user5@example.com', 'Tower', 13, 14);

-- Get the Top 3 Users with the Most towers placed on the Map
SELECT users.Username, COUNT(*) AS BuildingCount    -- Select the username from the users table and count the number of buildings for each user, naming the count 'BuildingCount'
    FROM users                                      -- Start with the users table as the base for the query
    JOIN playerbuildings ON users.Email = playerbuildings.Email  -- Combine information from the playerbuildings table by matching rows where the Email in both tables is the same
    WHERE BuildingName = 'Tower'                   -- Filter the combined results to only include rows where the BuildingName is 'Tower'
    GROUP BY users.Email                            -- Group the filtered results by the user's email to count the number of towers each user has
    ORDER BY BuildingCount DESC                     -- Sort the grouped results in descending order based on the number of towers
    LIMIT 3;