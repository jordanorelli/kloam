package main

import (
	"context"
	"io/ioutil"
	"time"

	"github.com/gorilla/websocket"
	"github.com/jordanorelli/blammo"
)

type player struct {
	*blammo.Log
	conn *websocket.Conn
	id   int
}

func (p *player) run() {
	ctx, cancel := context.WithCancel(context.Background())
	p.readMessages(cancel)
	go p.writeMessages(ctx)

	p.conn.Close()
}

func (p *player) readMessages(cancel func()) {
	defer cancel()
	for {
		_, rd, err := p.conn.NextReader()
		if err != nil {
			if closeError, ok := err.(*websocket.CloseError); ok {
				switch closeError.Code {
				case websocket.CloseNormalClosure:
					p.Info("client disconnected: %s", closeError.Text)
				default:
					p.Error("client disconnected weirdly: %#v", *closeError)
				}
			} else {
				p.Error("nextreader error: %s", err)
			}
			return
		}

		b, err := ioutil.ReadAll(rd)
		if err != nil {
			p.Error("read error: %s", err)
		} else {
			p.Child("rcv").Info(string(b))
		}
	}
}

func (p *player) writeMessages(ctx context.Context) {
	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ticker.C:
			w, err := p.conn.NextWriter(websocket.TextMessage)
			if err != nil {

			}
			w.Write([]byte("tick"))
			w.Close()
		case <-ctx.Done():
			return
		}
	}
}
