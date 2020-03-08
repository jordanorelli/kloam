package db

import (
	"database/sql"
	"fmt"
	"os"

	_ "github.com/mattn/go-sqlite3"
)

type SQLite struct {
	db *sql.DB
}

func OpenSQLite(path string) (*SQLite, error) {
	db, err := sql.Open("sqlite3", path)
	if err != nil {
		return nil, fmt.Errorf("unable to open sqlite3 database at %s: %w", path, err)
	}
	if _, err := db.Exec(`pragma foreign_keys = on;`); err != nil {
		fmt.Fprintf(os.Stderr, "failed to enforce foreign key constraints: %v\n", err)
	}

	if _, err := db.Exec(`
	create table if not exists players (
		id integer primary key autoincrement,
		name text unique not null,
		phash text not null,
		psalt text not null
	);`); err != nil {
		fmt.Fprintf(os.Stderr, "failed to create players table: %v\n", err)
	}

	if _, err := db.Exec(`
	create table if not exists bodies (
		id integer primary key autoincrement,
		player integer not null,
		x real not null,
		y real not null,
		z real not null,
		died_at datetime default current_timestamp not null,
		found_at datetime,
		found_by integer,
		foreign key (player) references players(id),
		foreign key (found_by) references players(id)
	);`); err != nil {
		fmt.Fprintf(os.Stderr, "failed to create bodies table: %v\n", err)
	}

	return &SQLite{db: db}, nil
}

func (db *SQLite) CreatePlayer(p *Player) error {
	if _, err := db.db.Exec(`
	insert into players (name, phash, psalt)
	values (?, ?, ?);
	`, p.Name, p.Hash, p.Salt); err != nil {
		return fmt.Errorf("unable to insert user: %w", err)
	}

	row := db.db.QueryRow(`select id from players where name = ?`, p.Name)
	if err := row.Scan(&p.ID); err != nil {
		return fmt.Errorf("unable to scan user ID: %w", err)
	}

	return nil
}

func (db *SQLite) ReadPlayer(p *Player) error {
	args := make([]interface{}, 0, 1)
	q := `select id, name, phash, psalt from players `
	if p.ID != 0 {
		q += `where id = ?`
		args = append(args, p.ID)
	} else {
		q += `where name = ?`
		args = append(args, p.Name)
	}
	row := db.db.QueryRow(q, args...)
	if err := row.Scan(&p.ID, &p.Name, &p.Hash, &p.Salt); err != nil {
		return fmt.Errorf("unable to read player row: %w", err)
	}
	return nil
}

func (db *SQLite) UpdatePlayer(p *Player) error {
	q := `update players set name = ?, phash = ?, pstalt = ? where id = ?`
	args := []interface{}{p.Name, p.Hash, p.Salt, p.ID}
	if _, err := db.db.Exec(q, args...); err != nil {
		return fmt.Errorf("unable to update player %s: %w", *p, err)
	}
	return nil
}

func (db *SQLite) Close() error { return db.db.Close() }
