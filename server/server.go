package main

import (
	"io/ioutil"
	"net/http"

	"github.com/gorilla/mux"
	"github.com/gorilla/websocket"
	"github.com/jordanorelli/blammo"
)

type server struct {
	*blammo.Log
	router *mux.Router
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
	defer conn.Close()

	s.Info("client connected: %v", conn.RemoteAddr())

	for {
		_, rd, err := conn.NextReader()
		if err != nil {
			s.Error("nextreader error: %s", err)
			break
		}

		b, err := ioutil.ReadAll(rd)
		if err != nil {
			s.Error("read error: %s", err)
		} else {
			s.Child("rcv").Info(string(b))
		}
	}

}
