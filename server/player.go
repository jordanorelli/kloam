package main

import (
	"context"
	"fmt"
	"io/ioutil"
	"time"

	"github.com/gorilla/websocket"
	"github.com/jordanorelli/blammo"
)

type player struct {
	*blammo.Log
	conn   *websocket.Conn
	server *server
	id     int
}

func (p *player) run() {
	p.Info("starting run cycle")
	defer p.Info("run cycle done")

	ctx, cancel := context.WithCancel(context.Background())
	go p.writeMessages(ctx)
	p.readMessages(cancel)
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
		p.server.inbox <- message{
			from: p.id,
			text: string(b),
		}
	}
}

func (p *player) writeMessages(ctx context.Context) {
	p.Info("writeMessages loop start")
	defer p.Info("writeMessage loop end")

	ticker := time.NewTicker(1 * time.Second)
	n := 0

	for {
		select {
		case t := <-ticker.C:
			n++
			p.Info("trying to write a tick")
			w, err := p.conn.NextWriter(websocket.TextMessage)
			if err != nil {
				p.Error("error getting writer: %v", err)
				return
			}

			fmt.Fprintf(w, "%d tick: %v", n, t)
			if err := w.Close(); err != nil {
				p.Error("close frame error: %v", err)
				return
			}
			p.Info("wrote a tick")
		case <-ctx.Done():
			return
		}
	}
}
