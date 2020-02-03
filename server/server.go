package main

import (
	"encoding/json"
	"net/http"
	"strconv"

	"github.com/gorilla/mux"
	"github.com/gorilla/websocket"
	"github.com/jordanorelli/blammo"
)

type server struct {
	*blammo.Log
	router  *mux.Router
	players map[int]*player
	join    chan player
	inbox   chan message
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
	case m := <-s.inbox:
		s.Info("received message: %v", m)
		var req request
		if err := json.Unmarshal([]byte(m.text), &req); err != nil {
			s.Error("dunno how to read this message: %v", m.text)
			return
		}
		cmd := req.parse(m.text)
		s.Info("cmd: %#v", cmd)
	}
}
