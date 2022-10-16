--The main table 
CREATE TABLE R_CURRENCY (
    ID int IDENTITY(1,1) PRIMARY KEY,
    TITLE varchar(60) NOT NULL,
    CODE varchar(3) NOT NULL,
    VALUE numeric(18,2) NOT NULL,
    A_DATE date NOT NULL
);
