-- Table: users
CREATE TABLE users (
                       username VARCHAR(255) PRIMARY KEY,
                       password VARCHAR(255) NOT NULL,
                       coins INTEGER NOT NULL DEFAULT 20,
                       elo INTEGER NOT NULL DEFAULT 100,
                       bio TEXT DEFAULT '',
                       image TEXT DEFAULT '',
                       name TEXT,
                       games_played INTEGER DEFAULT 0,
                       wins INTEGER DEFAULT 0,
                       losses INTEGER DEFAULT 0
);

-- Table: cards
CREATE TABLE cards (
                       id UUID PRIMARY KEY,
                       name TEXT NOT NULL,
                       damage DOUBLE PRECISION NOT NULL,
                       element_type TEXT NOT NULL CHECK (element_type IN ('Fire', 'Water', 'Normal')),
                       type TEXT NOT NULL CHECK (type IN ('Monster', 'Spell')),
                       owner TEXT REFERENCES users(username) ON DELETE SET NULL,
                       in_deck BOOLEAN DEFAULT FALSE
);

-- Table: packages
CREATE TABLE packages (
                          package_id UUID NOT NULL,
                          card_id UUID NOT NULL,
                          PRIMARY KEY (package_id, card_id),
                          CONSTRAINT packages_card_id_fkey FOREIGN KEY (card_id) REFERENCES cards(id) ON DELETE CASCADE
);
