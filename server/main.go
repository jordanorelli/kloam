package main

import (
	"net"
	"net/http"
	"os"

	"github.com/jordanorelli/blammo"
)

func main() {
	stdout := blammo.NewLineWriter(os.Stdout)
	stderr := blammo.NewLineWriter(os.Stderr)
	log := blammo.NewLog("kloam", blammo.DebugWriter(stdout), blammo.InfoWriter(stdout), blammo.ErrorWriter(stderr))

	s := server{
		Log:  log,
		join: make(chan player),
	}
	s.init()
	go s.run()
	lis, err := net.Listen("tcp", "0.0.0.0:9001")
	if err != nil {
		log.Error("listen error: %v", err)
		return
	}
	log.Info("listening on %v", lis.Addr())
	http.Serve(lis, s.handler())
}
