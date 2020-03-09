package db

import (
	"fmt"
	"time"
)

type Body struct {
	ID       int
	PlayerID int
	X        float64
	Y        float64
	Z        float64
	DiedAt   time.Time
	FoundAt  *time.Time
	FoundBy  *int
}

func (db *SQLite) ListBodies() ([]Body, error) {
	rows, err := db.db.Query(`select id, player, x, y, z, died_at from bodies where found_at is null`)
	if err != nil {
		return nil, fmt.Errorf("failed to fetch bodies from db: %w", err)
	}

	bodies := make([]Body, 0, 64)
	for rows.Next() {
		var body Body
		if err := rows.Scan(&body.ID, &body.PlayerID, &body.X, &body.Y, &body.Z, &body.DiedAt); err != nil {
			return nil, fmt.Errorf("failed to read bodies response rows: %w", err)
		}
		bodies = append(bodies, body)
	}
	return bodies, nil
}

func (db *SQLite) AddBody(body *Body) error {
	_, err := db.db.Exec(`insert into bodies
		(player, x, y, z)
		values (?, ?, ?, ?);`, body.PlayerID, body.X, body.Y, body.Z)
	if err != nil {
		return fmt.Errorf("failed to add body to db: %w", err)
	}
	return nil
}

func (db *SQLite) ReadBody(body *Body) error {
	args := make([]interface{}, 0, 1)
	q := `select id, player, x, y, z, died_at from bodies`
	if body.ID != 0 {
		q += ` where id = ?`
		args = append(args, body.ID)
	} else {
		q += ` where player = ?`
		args = append(args, body.PlayerID)
	}
	q += ` and found_at is null`

	row := db.db.QueryRow(q, args...)
	if err := row.Scan(&body.ID, &body.PlayerID, &body.X, &body.Y, &body.Z, &body.DiedAt); err != nil {
		return fmt.Errorf("failed to read body from db: %w", err)
	}
	return nil
}

func (db *SQLite) FindBody(id, finder int) error {
	q := `update bodies set found_at = CURRENT_TIMESTAMP, found_by = ? where id = ?`
	_, err := db.db.Exec(q, finder, id)
	if err != nil {
		return fmt.Errorf("unable to claim body %d as found by %d: %w", id, finder, err)
	}
	return nil
}
