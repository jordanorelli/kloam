package main

import "github.com/gorilla/websocket"

type player struct {
	conn *websocket.Conn
}

func (p *player) readMessages() {
}
