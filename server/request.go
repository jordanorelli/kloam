package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/jordanorelli/kloam/db"
)

type request struct {
	Cmd string `json:"cmd"`
}

func (r request) parse(msg string) command {
	var c command
	switch r.Cmd {
	case "login":
		c = new(login)
	case "death":
		c = new(death)
	case "collect-soul":
		c = new(collectSoul)
	default:
		return nil
	}
	json.Unmarshal([]byte(msg), &c)
	return c
}

type vector3 struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

type command interface {
	exec(*server, *player)
}

type collectSoul struct {
	PlayerName string  `json:"playerName"`
	Position   vector3 `json:"position"`
}

func (c *collectSoul) exec(s *server, from *player) {
	soul, ok := s.souls[c.PlayerName]
	if !ok {
		return
	}
	delete(s.souls, c.PlayerName)

	b, err := json.Marshal(soul)
	if err != nil {
		s.Error("unable to serialize soul: %v", err)
		return
	}

	msg := fmt.Sprintf("soul-collected %s", string(b))
	for _, player := range s.players {
		select {
		case player.outbox <- msg:
		default:
			s.Error("can't write to player %s's outbox", player.username)
		}
	}
}

type login struct {
	Username string `json:"username"`
	Password string `json:"password"`
}

type loginResult struct {
	Passed bool   `json:"passed"`
	Error  string `json:"error,omitempty"`
}

func (l *login) exec(s *server, from *player) {
	sendResult := func(res loginResult) {
		b, _ := json.Marshal(res)
		msg := fmt.Sprintf("login-result %s", string(b))
		from.outbox <- msg
	}

	row := db.Player{Name: l.Username}
	if err := s.db.ReadPlayer(&row); err != nil {
		sendResult(loginResult{
			Error: fmt.Sprintf("failed to read player from database: %v", err),
		})
		return
	}
	fmt.Printf("login read row from database: %v\n", row)

	if row.HasPassword(l.Password) {
		sendResult(loginResult{Passed: true})
		from.username = l.Username
		from.id = row.ID
	} else {
		sendResult(loginResult{Error: "bad password"})
		return
	}

	messages := make([]string, 0, len(s.souls))

	for _, soul := range s.souls {
		b, _ := json.Marshal(soul)
		msg := fmt.Sprintf("spawn-soul %s", string(b))
		messages = append(messages, msg)
	}

	go func() {
		time.Sleep(1 * time.Second)

		for _, msg := range messages {
			select {
			case from.outbox <- msg:
			default:
			}
		}
	}()
}

type death struct {
	Position vector3 `json:"position"`
}

func (d *death) exec(s *server, from *player) {
	s.Info("executing a death: %#v", d)

	body := &db.Body{
		PlayerID: from.id,
		X:        d.Position.X,
		Y:        d.Position.Y,
		Z:        d.Position.Z,
	}
	s.Info("adding body: %#v", body)
	if err := s.db.AddBody(body); err != nil {
		s.Error("db error: %v", err)
	}

	_soul := soul{
		PlayerName: from.username,
		Position:   d.Position,
	}
	s.souls[from.username] = _soul

	b, err := json.Marshal(_soul)
	if err != nil {
		s.Error("unable to serialize soul: %v", err)
		return
	}

	msg := fmt.Sprintf("spawn-soul %s", string(b))

	for _, player := range s.players {
		select {
		case player.outbox <- msg:
		default:
			s.Error("can't write to player %s's outbox", player.username)
		}
	}
}
