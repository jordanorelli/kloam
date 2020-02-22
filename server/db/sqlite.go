package db

import (
	"database/sql"
	"fmt"
	"os"

	_ "github.com/mattn/go-sqlite3"
	"golang.org/x/crypto/bcrypt"
)

type SQLite struct {
	db *sql.DB
}

func OpenSQLite(path string) (*SQLite, error) {
	db, err := sql.Open("sqlite3", path)
	if err != nil {
		return nil, fmt.Errorf("unable to open sqlite3 database at %s: %v", path, err)
	}

	if _, err := db.Exec(`
	create table if not exists users (
		id integer primary key autoincrement,
		name text unique not null,
		phash text not null,
		psalt text not null
	);`); err != nil {
		fmt.Fprintf(os.Stderr, "failed to create users table: %v\n", err)
	}

	return &SQLite{db: db}, nil
}

func (db *SQLite) CreateUser(name, pass, salt string) error {
	combined := []byte(pass + salt)
	hashBytes, err := bcrypt.GenerateFromPassword(combined, 13)
	if err != nil {
		return fmt.Errorf("unable to generate password hash: %v", err)
	}
	hash := string(hashBytes)
	if _, err := db.db.Exec(`
	insert into users (name, phash, psalt)
	values (?, ?, ?);
	`, name, hash, salt); err != nil {
		return fmt.Errorf("unable to insert user: %v", err)
	}
	return nil
}

func (db *SQLite) CheckPassword(name, pass string) error {
	rows, err := db.db.Query(`
	select phash, psalt from users where name = ?;
	`, name)
	if err != nil {
		return fmt.Errorf("failed to fetch row for user %s: %v", name, err)
	}
	defer rows.Close()

	scannedRows := 0
	for rows.Next() {
		var (
			dbhash string
			dbsalt string
		)
		if err := rows.Scan(&dbhash, &dbsalt); err != nil {
			return fmt.Errorf("failed to scan row: %v", err)
		}
		scannedRows++
		if err := bcrypt.CompareHashAndPassword([]byte(dbhash), []byte(pass+dbsalt)); err != nil {
			return fmt.Errorf("failed hash match: %v", err)
		}
	}
	if scannedRows == 0 {
		return fmt.Errorf("no such user")
	}

	return nil
}

func (db *SQLite) Close() error { return db.db.Close() }
