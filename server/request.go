package main

import (
	"encoding/json"
	"fmt"
	"time"
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
	res := loginResult{}
	if err := s.db.CheckPassword(l.Username, l.Password); err != nil {
		res.Error = err.Error()
	} else {
		res.Passed = true
		from.username = l.Username

	}
	b, _ := json.Marshal(res)
	msg := fmt.Sprintf("login-result %s", string(b))
	from.outbox <- msg

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
