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
