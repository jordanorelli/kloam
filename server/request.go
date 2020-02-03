package main

import (
	"encoding/json"
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
	exec(*server)
}

type collectSoul struct {
	PlayerName string  `json:"playerName"`
	Position   vector3 `json:"position"`
}

func (c *collectSoul) exec(s *server) {
}

type login struct {
	Username string `json:"username"`
	Password string `json:"password"`
}

func (l *login) exec(s *server) {
}

type death struct {
	Position vector3 `json:"position"`
}

func (d *death) exec(s *server) {
}
