-- Создаем таблицу Users
CREATE TABLE Users (
	ID_User INTEGER PRIMARY KEY,
	Login_User TEXT NOT NULL,
	Password_User TEXT NOT NULL
);

-- Создаем таблицу Topics
CREATE TABLE Topics (
	ID_Topic INTEGER PRIMARY KEY,
	Name_Topic TEXT NOT NULL,
	Path_Topic TEXT NOT NULL,
	Latitude_Topic REAL NOT NULL,
	Longitude_Topic REAL NOT NULL,
	Altitude_Topic REAL NOT NULL,
	AltitudeSensor_Topic REAL NOT NULL,
	CheckTime_Topic DATETIME
);

-- Создаем таблицу Data с каскадным удалением
CREATE TABLE Data (
	ID_Data INTEGER PRIMARY KEY,
	ID_Topic INTEGER NOT NULL,
	Value_Data TEXT,
	Time_Data DATETIME NOT NULL,
	FOREIGN KEY (ID_Topic) REFERENCES Topics(ID_Topic) ON DELETE CASCADE
);

-- Создаем таблицу AreaPoints с каскадным удалением
CREATE TABLE AreaPoints (
	ID_AreaPoint INTEGER PRIMARY KEY,
	ID_Topic INTEGER NOT NULL,
	Depression_AreaPoint TEXT NOT NULL,
	Perimeter_AreaPoint TEXT NOT NULL,
	Included_AreaPoint TEXT NOT NULL,
	Islands_AreaPoint TEXT NOT NULL,
	FOREIGN KEY (ID_Topic) REFERENCES Topics(ID_Topic) ON DELETE CASCADE
);
