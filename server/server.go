package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"

	"github.com/gorilla/mux"
	"github.com/gorilla/websocket"
	"github.com/jordanorelli/blammo"
	"github.com/jordanorelli/kloam/db"
)

type server struct {
	*blammo.Log
	router  *mux.Router
	players map[int]*player
	join    chan player
	leave   chan *player
	inbox   chan message
	souls   map[string]soul
	db      *db.SQLite
}

func (s *server) init() {
	r := mux.NewRouter()
	r.HandleFunc("/kloam", s.play)

	s.router = r
}

func (s *server) handler() http.Handler {
	return s.router
}

func (s *server) play(w http.ResponseWriter, r *http.Request) {
	u := websocket.Upgrader{}

	conn, err := u.Upgrade(w, r, nil)
	if err != nil {
		s.Error("upgrade error: %s", err)
		return
	}

	s.Info("client connected: %v", conn.RemoteAddr())
	s.join <- player{
		conn:   conn,
		server: s,
		outbox: make(chan string, 8),
	}
}

func (s *server) run() {
	s.players = make(map[int]*player)

	for pc := 0; true; pc++ {
		s.step(pc)
	}
}

func (s *server) step(pc int) {
	select {
	case p := <-s.join:
		s.Info("received join: %#v", p)
		p.id = pc
		p.Log = s.Child("players").Child(strconv.Itoa(p.id))
		go p.run()
		s.players[p.id] = &p
		for _, soul := range s.souls {
			b, _ := json.Marshal(soul)
			msg := fmt.Sprintf("spawn-soul %s", string(b))
			select {
			case p.outbox <- msg:
			default:
			}
		}
	case p := <-s.leave:
		delete(s.players, p.id)

	case m := <-s.inbox:
		s.Info("received message: %v", m)
		var req request
		if err := json.Unmarshal([]byte(m.text), &req); err != nil {
			s.Error("dunno how to read this message: %v", m.text)
			return
		}
		cmd := req.parse(m.text)
		s.Info("cmd: %#v", cmd)
		cmd.exec(s, m.from)
	}
}
